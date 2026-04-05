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

		public OrderDetailsFactory(
			IOrderInventoryManager inventoryManager,
			IUnitOfWork unitOfWork)
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

			var stockItems = items
				.GroupBy(i => i.VariantId)
				.Select(g => (VariantId: g.Key, Quantity: g.Sum(x => x.Quantity)))
				.ToList();
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation)
			{
				throw AppException.BadRequest("Stock validation failed. Some items might be out of stock.");
			}

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);

			var variantDictionary = variants.ToDictionary(v => v.Id);
			decimal explicitDiscountTotal = 0m;

			var batchStockTracker = new Dictionary<Guid, int>();

			foreach (var (VariantId, BatchId, Quantity, LineDiscount, LineFinalTotal) in items)
			{
				if (!variantDictionary.TryGetValue(VariantId, out var variant))
					throw AppException.NotFound($"Product variant {VariantId} not found.");

				decimal unitPrice = variant.BasePrice;
				var lineSubtotal = unitPrice * Quantity;

				if (LineFinalTotal.HasValue && LineFinalTotal.Value > lineSubtotal)
					throw AppException.BadRequest("Final total amount cannot exceed subtotal.");

				if (BatchId.HasValue)
				{
					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.Id == BatchId.Value && b.VariantId == VariantId)
						?? throw AppException.NotFound($"Batch {BatchId.Value} not found for variant {VariantId}.");

					if (!batchStockTracker.ContainsKey(batch.Id))
						batchStockTracker[batch.Id] = batch.AvailableInBatch;

					if (batchStockTracker[batch.Id] < Quantity)
						throw AppException.Conflict($"Data inconsistency: Batch {batch.Id} does not have enough stock for variant {VariantId}. Need {Quantity}, available {batchStockTracker[batch.Id]}.");

					batchStockTracker[batch.Id] -= Quantity;

					var totalLineDiscount = LineFinalTotal.HasValue
						? lineSubtotal - LineFinalTotal.Value
						: LineDiscount;

					totalLineDiscount = Math.Max(0m, Math.Min(totalLineDiscount, lineSubtotal));
					explicitDiscountTotal += totalLineDiscount;

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
						FinalUnitPrice = Quantity > 0
							? (lineSubtotal - totalLineDiscount) / Quantity
							: 0m
					};

					string snapshotJson = JsonSerializer.Serialize(snapshotData);

					order.AddOrderDetail(
						 VariantId,
						 Quantity,
						 unitPrice,
						 snapshotJson);

					if (totalLineDiscount > 0)
					{
						var createdOrderDetail = order.OrderDetails.Last();
						createdOrderDetail.ApplyDiscount(totalLineDiscount);
					}

					continue;
				}

				var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(VariantId);
				var remainingToAllocate = Quantity;
				var allocations = new List<(Batch Batch, int Quantity)>();

				foreach (var batch in availableBatches)
				{
					if (remainingToAllocate <= 0)
						break;

					if (!batchStockTracker.ContainsKey(batch.Id))
						batchStockTracker[batch.Id] = batch.AvailableInBatch;

					var availableInBatch = batchStockTracker[batch.Id];
					if (availableInBatch <= 0) continue;

					var quantityFromBatch = Math.Min(remainingToAllocate, availableInBatch);
					allocations.Add((batch, quantityFromBatch));
					remainingToAllocate -= quantityFromBatch;

					batchStockTracker[batch.Id] -= quantityFromBatch;
				}

				if (remainingToAllocate > 0)
					throw AppException.Conflict($"Data inconsistency: Batches do not have enough stock for variant {VariantId}. Need {Quantity}, missing {remainingToAllocate}.");

				foreach (var (batch, allocatedQuantity) in allocations)
				{
					var detailLineTotal = unitPrice * allocatedQuantity;

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
						FinalUnitPrice = allocatedQuantity > 0
							? detailLineTotal / allocatedQuantity
							: 0m
					};

					string snapshotJson = JsonSerializer.Serialize(snapshotData);

					order.AddOrderDetail(
						 VariantId,
						 allocatedQuantity,
						 unitPrice,
						 snapshotJson);
				}
			}

			var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
			if (subtotal <= 0)
				return;

			if (explicitDiscountTotal > 0)
			{
				if (finalTotalAmount.HasValue)
				{
					var expectedDiscountTotal = subtotal - finalTotalAmount.Value;
					if (expectedDiscountTotal < 0)
						throw AppException.BadRequest("Final total amount cannot exceed subtotal.");

					var delta = expectedDiscountTotal - explicitDiscountTotal;
					if (Math.Abs(delta) > 0.0001m)
					{
						var lastDetail = order.OrderDetails.Last();
						var currentDiscount = lastDetail.ApportionedDiscount;
						var lineTotal = lastDetail.UnitPrice * lastDetail.Quantity;
						var adjustedDiscount = Math.Max(0m, Math.Min(currentDiscount + delta, lineTotal));
						lastDetail.ApplyDiscount(adjustedDiscount);
					}
				}

				return;
			}

			var effectiveFinalTotal = finalTotalAmount ?? order.TotalAmount;
			var totalDiscount = subtotal - effectiveFinalTotal;

			if (totalDiscount <= 0)
				return;

			if (totalDiscount > subtotal)
				throw AppException.BadRequest("Total discount cannot exceed subtotal.");

			decimal apportionedTotal = 0m;
			for (var i = 0; i < order.OrderDetails.Count; i++)
			{
				var orderDetail = order.OrderDetails.ElementAt(i);
				var lineTotal = orderDetail.UnitPrice * orderDetail.Quantity;

				var apportionedDiscount = i == order.OrderDetails.Count - 1
					? totalDiscount - apportionedTotal
					: Math.Round((lineTotal / subtotal) * totalDiscount, 2, MidpointRounding.AwayFromZero);

				apportionedDiscount = Math.Min(apportionedDiscount, lineTotal);
				orderDetail.ApplyDiscount(apportionedDiscount);
				apportionedTotal += apportionedDiscount;
			}
		}
	}
}
