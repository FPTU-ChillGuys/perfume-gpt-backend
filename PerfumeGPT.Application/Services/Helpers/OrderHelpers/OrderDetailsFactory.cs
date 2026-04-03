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
			List<(Guid VariantId, int Quantity, decimal LineDiscount)> items,
			decimal? finalTotalAmount = null)
		{
			if (order == null) throw AppException.BadRequest("Order is required.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Order items are required.");

			var stockItems = items.Select(i => (i.VariantId, i.Quantity)).ToList();
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(stockItems);
			if (!stockValidation)
			{
				throw AppException.BadRequest("Stock validation failed. Some items might be out of stock.");
			}

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);

			var variantDictionary = variants.ToDictionary(v => v.Id);

			foreach (var (VariantId, Quantity, LineDiscount) in items)
			{
				if (!variantDictionary.TryGetValue(VariantId, out var variant))
					throw AppException.NotFound($"Product variant {VariantId} not found.");

				decimal unitPrice = variant.BasePrice;

				var snapshotData = new
				{
					variant.Sku,
					variant.Barcode,
					ProductName = variant.Product?.Name ?? "Unknown Product",
					variant.VolumeMl,
					VariantType = variant.Type.ToString(),
					Concentration = variant.Concentration?.Name
				};

				string snapshotJson = JsonSerializer.Serialize(snapshotData);

				order.AddOrderDetail(
					 VariantId,
					 Quantity,
					 unitPrice,
					 snapshotJson);

				if (LineDiscount > 0)
				{
					var createdOrderDetail = order.OrderDetails.Last();
					var lineTotal = createdOrderDetail.UnitPrice * createdOrderDetail.Quantity;
					var safeDiscount = Math.Min(LineDiscount, lineTotal);
					createdOrderDetail.ApplyDiscount(safeDiscount);
				}
			}

			var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
			if (subtotal <= 0)
				return;

			var explicitDiscountTotal = items.Sum(x => Math.Max(0m, x.LineDiscount));
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
