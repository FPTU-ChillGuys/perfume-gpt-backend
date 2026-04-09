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

		public async Task CreateOrderDetailsAsync(
			Order order,
			List<(Guid VariantId, Guid? BatchId, int Quantity, decimal LineDiscount, decimal? LineFinalTotal)> items,
			decimal? finalTotalAmount = null)
		{
			if (order == null) throw AppException.BadRequest("Order is required.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Order items are required.");

			var stockItems = items.GroupBy(i => i.VariantId).Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity))).ToList();
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation) throw AppException.BadRequest("Stock validation failed.");

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);
			var variantDictionary = variants.ToDictionary(v => v.Id);
			var batchStockTracker = new Dictionary<Guid, int>();

			foreach (var (VariantId, BatchId, Quantity, LineDiscount, LineFinalTotal) in items)
			{
				if (!variantDictionary.TryGetValue(VariantId, out var variant))
					throw AppException.NotFound($"Product variant {VariantId} not found.");

				decimal unitPrice = variant.BasePrice;
				var lineSubtotal = unitPrice * Quantity;

				// Lấy chính xác số tiền Discount từ Pricing Engine
				var totalLineDiscount = LineFinalTotal.HasValue ? lineSubtotal - LineFinalTotal.Value : LineDiscount;
				totalLineDiscount = Math.Max(0m, Math.Min(totalLineDiscount, lineSubtotal));

				// TRƯỜNG HỢP 1: Có Batch cụ thể (Do Campaign hoặc user tự chọn)
				if (BatchId.HasValue)
				{
					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.Id == BatchId.Value && b.VariantId == VariantId)
						?? throw AppException.NotFound($"Batch {BatchId.Value} not found.");

					if (!batchStockTracker.ContainsKey(batch.Id)) batchStockTracker[batch.Id] = batch.AvailableInBatch;
					if (batchStockTracker[batch.Id] < Quantity) throw AppException.Conflict($"Batch {batch.Id} stock shortage.");

					batchStockTracker[batch.Id] -= Quantity;

					string snapshotJson = CreateSnapshotJson(variant, batch, Quantity, lineSubtotal, totalLineDiscount);
					order.AddOrderDetail(VariantId, Quantity, unitPrice, snapshotJson);

					if (totalLineDiscount > 0)
					{
						order.OrderDetails.Last().ApplyDiscount(totalLineDiscount);
					}
					continue;
				}

				// TRƯỜNG HỢP 2: Không có Batch (Cắt Lô tự động FIFO)
				var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(VariantId);
				var remainingToAllocate = Quantity;
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

				if (remainingToAllocate > 0) throw AppException.Conflict($"Stock shortage for variant {VariantId}.");

				// Phân bổ số tiền Khuyến mãi của cả dòng xuống cho từng phần Lô bị cắt nhỏ
				decimal allocatedDiscount = 0m;
				for (int i = 0; i < allocations.Count; i++)
				{
					var (batch, allocatedQuantity) = allocations[i];
					var detailLineTotal = unitPrice * allocatedQuantity;

					var isLast = i == allocations.Count - 1;
					var sliceDiscount = isLast
						? totalLineDiscount - allocatedDiscount // Lô cuối ôm phần tiền thừa (chống lệch xu)
						: Math.Round((detailLineTotal / lineSubtotal) * totalLineDiscount, 2, MidpointRounding.AwayFromZero);

					sliceDiscount = Math.Max(0m, Math.Min(sliceDiscount, detailLineTotal));
					allocatedDiscount += sliceDiscount;

					string snapshotJson = CreateSnapshotJson(variant, batch, allocatedQuantity, detailLineTotal, sliceDiscount);
					order.AddOrderDetail(VariantId, allocatedQuantity, unitPrice, snapshotJson);

					if (sliceDiscount > 0)
					{
						order.OrderDetails.Last().ApplyDiscount(sliceDiscount);
					}
				}
			}

			// ĐÃ XÓA TOÀN BỘ ĐOẠN TÍNH TOÁN APPORTIONMENT CŨ CỦA BẠN VÌ PRICING ENGINE ĐÃ LÀM RỒI
		}

		// Tách hàm tạo Snapshot cho Clean Code
		private static string CreateSnapshotJson(ProductVariant variant, Batch batch, int quantity, decimal lineTotal, decimal discount)
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
				FinalUnitPrice = quantity > 0 ? (lineTotal - discount) / quantity : 0m
			};
			return JsonSerializer.Serialize(snapshotData);
		}
	}
}
