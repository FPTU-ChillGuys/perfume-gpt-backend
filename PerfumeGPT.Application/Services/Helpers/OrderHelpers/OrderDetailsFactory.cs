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

		public OrderDetailsFactory(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task CreateOrderDetailsAsync(Order order, List<CartCheckoutItemDto> items)
		{
			if (order == null) throw AppException.BadRequest("Đơn hàng là bắt buộc.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Danh sách sản phẩm của đơn hàng là bắt buộc.");

			// 1. Validate Tổng Tồn Kho
			var stockItems = items.GroupBy(i => i.VariantId).Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();
			var stockValidation = await ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation) throw AppException.BadRequest("Xác thực tồn kho thất bại.");

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);
			var variantDictionary = variants.ToDictionary(v => v.Id);
			var batchStockTracker = new Dictionary<Guid, int>();

			var orderDetailsToAdd = new List<OrderDetail>();

			foreach (var item in items)
			{
				if (!variantDictionary.TryGetValue(item.VariantId, out var variant))
					throw AppException.NotFound($"Biến thể sản phẩm {item.VariantId} không tồn tại.");

				// Lấy trực tiếp 2 số tiền giảm từ DTO do Engine tính toán
				var totalVoucherDiscount = item.ApportionedVoucherDiscount;

				// Tính ngược lại tiền Promotion: Tiền Promotion = Tổng giảm - Tiền Voucher
				var totalPromoDiscount = item.Discount - totalVoucherDiscount;

				// TRƯỜNG HỢP 1: Có Batch cụ thể (Do Campaign hoặc user tự chọn ở POS)
				if (item.BatchId.HasValue)
				{
					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.Id == item.BatchId.Value && b.VariantId == item.VariantId)
						?? throw AppException.NotFound($"Lô hàng {item.BatchId.Value} cho biến thể {item.VariantId} không tồn tại.");

					if (!batchStockTracker.ContainsKey(batch.Id)) batchStockTracker[batch.Id] = batch.AvailableInBatch;
					if (batchStockTracker[batch.Id] < item.Quantity) throw AppException.Conflict($"Lô hàng {batch.BatchCode} không đủ tồn để cung cấp cho biến thể {item.VariantId}.");

					batchStockTracker[batch.Id] -= item.Quantity;

					string snapshotJson = CreateSnapshotJson(variant, batch, item.Quantity, item.SubTotal, totalPromoDiscount);

					//  GỌI HÀM CREATE MỚI VỚI ĐẦY ĐỦ THAM SỐ
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

				if (remainingToAllocate > 0) throw AppException.Conflict($"Tồn kho không đủ để cung cấp cho biến thể {item.VariantId}. Thiếu {remainingToAllocate} đơn vị.");

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

					// TẠO DÒNG CHI TIẾT
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

		private async Task<bool> ValidateStockAvailabilityAsync(List<(Guid VariantId, int Quantity)> items)
		{
			if (items == null || items.Count == 0) return true;

			// BƯỚC 2: Gom nhóm các Item trùng VariantId (Đề phòng giỏ hàng gửi lên 2 dòng có cùng Variant)
			var groupedItems = items
				.GroupBy(i => i.VariantId)
				.Select(g => new { VariantId = g.Key, RequiredQty = g.Sum(x => x.Quantity) })
				.ToList();

			var variantIds = groupedItems.Select(x => x.VariantId).Distinct().ToList();

			// BƯỚC 3: BULK READ - Kéo tất cả dữ liệu liên quan lên RAM chỉ bằng 3 câu Query (Có AsNoTracking để tối ưu RAM)

			// 3.1. Kéo Stock
			var stocks = await _unitOfWork.Stocks.GetAllAsync(
				s => variantIds.Contains(s.VariantId),
				asNoTracking: true);
			var stockDict = stocks.ToDictionary(s => s.VariantId);

			// 3.2. Kéo Batch (Chỉ lấy các lô chưa hết hạn)
			var now = DateTime.UtcNow;
			var batches = await _unitOfWork.Batches.GetAllAsync(
				b => variantIds.Contains(b.VariantId) && b.ExpiryDate > now,
				asNoTracking: true);

			// Tính tổng số lượng khả dụng của tất cả các Lô gộp lại cho từng Variant
			var batchDict = batches
				.GroupBy(b => b.VariantId)
				.ToDictionary(
					g => g.Key,
					g => g.Sum(b => Math.Max(0, b.RemainingQuantity - b.ReservedQuantity)));

			// 3.3. Kéo Variant (Chỉ để lấy Sku phục vụ cho việc in ra câu báo lỗi)
			var variants = await _unitOfWork.Variants.GetAllAsync(
				v => variantIds.Contains(v.Id),
				asNoTracking: true);
			var variantDict = variants.ToDictionary(v => v.Id);

			// BƯỚC 4: THỰC HIỆN ĐỐI CHIẾU TRÊN RAM (IN-MEMORY VALIDATION)
			foreach (var item in groupedItems)
			{
				var variantName = variantDict.TryGetValue(item.VariantId, out var v) ? $"Variant {v.Sku}" : "Unknown product";

				// 4.1. Đối chiếu tổng tồn kho (Stock)
				if (!stockDict.TryGetValue(item.VariantId, out var stock) || stock.AvailableQuantity < item.RequiredQty)
				{
					throw AppException.BadRequest($"Không đủ hàng tồn kho cho {variantName}.");
				}

				// 4.2. Đối chiếu Tồn kho theo lô (Batch)
				if (!batchDict.TryGetValue(item.VariantId, out var totalBatchAvailable) || totalBatchAvailable < item.RequiredQty)
				{
					throw AppException.BadRequest($"Không đủ hàng tồn kho trong lô cho {variantName}.");
				}
			}

			return true;
		}
	}
}
