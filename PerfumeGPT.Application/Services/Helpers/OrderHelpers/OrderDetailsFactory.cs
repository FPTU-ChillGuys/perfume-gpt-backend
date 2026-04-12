using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using System.Text.Json;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderDetailsFactory : IOrderDetailsFactory
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IOrderInventoryManager _inventoryManager;

		public OrderDetailsFactory(IOrderInventoryManager inventoryManager, IUnitOfWork unitOfWork)
		{
			_inventoryManager = inventoryManager;
			_unitOfWork = unitOfWork;
		}

		public async Task CreateOrderDetailsAsync(Order order, List<CartCheckoutItemDto> items)
		{
			if (order == null) throw AppException.BadRequest("Order is required.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Order items are required.");

			// 1. Validate Tổng Tồn Kho
			var stockItems = items.GroupBy(i => i.VariantId).Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation) throw AppException.BadRequest("Stock validation failed.");

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);
			var variantDictionary = variants.ToDictionary(v => v.Id);
			var batchStockTracker = new Dictionary<Guid, int>();

			var orderDetailsToAdd = new List<OrderDetail>();

			foreach (var item in items)
			{
				if (!variantDictionary.TryGetValue(item.VariantId, out var variant))
					throw AppException.NotFound($"Product variant {item.VariantId} not found.");

				// Lấy trực tiếp 2 số tiền giảm từ DTO do Engine tính toán
				var totalVoucherDiscount = item.ApportionedVoucherDiscount;

				// Tính ngược lại tiền Promotion: Tiền Promotion = Tổng giảm - Tiền Voucher
				var totalPromoDiscount = item.Discount - totalVoucherDiscount;

				// TRƯỜNG HỢP 1: Có Batch cụ thể (Do Campaign hoặc user tự chọn ở POS)
				if (item.BatchId.HasValue)
				{
					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.Id == item.BatchId.Value && b.VariantId == item.VariantId)
						?? throw AppException.NotFound($"Batch {item.BatchId.Value} not found.");

					if (!batchStockTracker.ContainsKey(batch.Id)) batchStockTracker[batch.Id] = batch.AvailableInBatch;
					if (batchStockTracker[batch.Id] < item.Quantity) throw AppException.Conflict($"Batch {batch.Id} stock shortage.");

					batchStockTracker[batch.Id] -= item.Quantity;

					string snapshotJson = CreateSnapshotJson(variant, batch, item.Quantity, item.SubTotal, totalPromoDiscount);

					// 💥 GỌI HÀM CREATE MỚI VỚI ĐẦY ĐỦ THAM SỐ
					var detail = OrderDetail.Create(
						item.VariantId,
						item.AppliedPromotionItemId,
						item.Quantity,
						variant.BasePrice,
						snapshotJson,
						totalVoucherDiscount, // Tiền gánh Voucher
						totalPromoDiscount    // Tiền giảm Promotion
					);
					orderDetailsToAdd.Add(detail);
					continue;
				}

				// TRƯỜNG HỢP 2: Không có Batch (Cắt Lô tự động FIFO - Chỉ xảy ra với hàng nguyên giá)
				var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(item.VariantId);
				var remainingToAllocate = item.Quantity;
				var allocations = new List<(Batch Batch, int AllocatedQty)>();

				foreach (var batch in availableBatches)
				{
					if (remainingToAllocate <= 0) break;
					if (!batchStockTracker.ContainsKey(batch.Id)) batchStockTracker[batch.Id] = batch.AvailableInBatch;

					var availableInBatch = batchStockTracker[batch.Id];
					if (availableInBatch <= 0) continue;

					var quantityFromBatch = Math.Min(remainingToAllocate, availableInBatch);
					allocations.Add((batch, quantityFromBatch));
					remainingToAllocate -= quantityFromBatch;
					batchStockTracker[batch.Id] -= quantityFromBatch;
				}

				if (remainingToAllocate > 0) throw AppException.Conflict($"Stock shortage for variant {item.VariantId}.");

				// Phân bổ 2 loại tiền giảm xuống cho các Lô con (Chỉ cần thiết nếu Voucher trải đều)
				decimal allocatedPromo = 0m;
				decimal allocatedVoucher = 0m;

				for (int i = 0; i < allocations.Count; i++)
				{
					var (batch, allocatedQuantity) = allocations[i];
					var detailLineTotal = variant.BasePrice * allocatedQuantity;
					var isLast = i == allocations.Count - 1;

					// Chia nhỏ tiền Promotion
					var slicePromo = isLast
						? totalPromoDiscount - allocatedPromo
						: Math.Round((detailLineTotal / item.SubTotal) * totalPromoDiscount, 0, MidpointRounding.AwayFromZero);
					allocatedPromo += slicePromo;

					// Chia nhỏ tiền Voucher
					var sliceVoucher = isLast
						? totalVoucherDiscount - allocatedVoucher
						: Math.Round((detailLineTotal / item.SubTotal) * totalVoucherDiscount, 0, MidpointRounding.AwayFromZero);
					allocatedVoucher += sliceVoucher;

					string snapshotJson = CreateSnapshotJson(variant, batch, allocatedQuantity, detailLineTotal, slicePromo);

					// 💥 TẠO DÒNG CHI TIẾT
					var detail = OrderDetail.Create(
						item.VariantId,
						item.AppliedPromotionItemId,
						allocatedQuantity,
						variant.BasePrice,
						snapshotJson,
						sliceVoucher,
						slicePromo
					);
					orderDetailsToAdd.Add(detail);
				}
			}

			// 3. Cuối cùng, nhét tất cả vào Order
			order.AddOrderDetails(orderDetailsToAdd);
		}

		private static string CreateSnapshotJson(ProductVariant variant, Batch batch, int quantity, decimal lineTotal, decimal promoDiscount)
		{
			var snapshotData = new
			{
				variant.Sku,
				variant.Barcode,
				ProductName = variant.Product?.Name ?? "Unknown Product",
				variant.VolumeMl,
				VariantType = variant.Type.ToString(),
				Concentration = variant.Concentration?.Name,
				BatchId = batch.Id,
				batch.BatchCode,
				batch.ExpiryDate,
				// Giá cuối cùng mà khách hàng thấy trên hóa đơn cho món này (Chỉ trừ Promotion, ko trừ Voucher)
				FinalUnitPrice = quantity > 0 ? (lineTotal - promoDiscount) / quantity : 0m
			};
			return JsonSerializer.Serialize(snapshotData);
		}
	}
}
