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

		public async Task CreateOrderDetailsAsync(Order order, List<(Guid VariantId, int Quantity)> items)
		{
			if (order == null) throw AppException.BadRequest("Order is required.");
			if (items == null || items.Count == 0) throw AppException.BadRequest("Order items are required.");

			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(items);
			if (!stockValidation)
			{
				throw AppException.BadRequest("Stock validation failed. Some items might be out of stock.");
			}

			var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
			var variants = await _unitOfWork.Variants.GetVariantsWithDetailsByIdsAsync(variantIds);

			var variantDictionary = variants.ToDictionary(v => v.Id);

			foreach (var (VariantId, Quantity) in items)
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
			}
		}
	}
}
