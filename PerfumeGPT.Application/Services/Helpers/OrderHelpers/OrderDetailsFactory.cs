using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderDetailsFactory : IOrderDetailsFactory
	{
		private readonly IVariantService _variantService;
		private readonly IOrderInventoryManager _inventoryManager;

		public OrderDetailsFactory(
			IVariantService variantService,
			IOrderInventoryManager inventoryManager)
		{
			_variantService = variantService;
			_inventoryManager = inventoryManager;
		}

		public async Task<List<OrderDetail>> CreateOrderDetailsAsync(List<(Guid VariantId, int Quantity)> items)
		{
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(items);
			if (!stockValidation)
			{
				throw AppException.BadRequest("Stock validation failed.");
			}

			var orderDetails = new List<OrderDetail>();
			foreach (var (VariantId, Quantity) in items)
			{
				var variant = await _variantService.GetVariantForCreateOrderAsync(VariantId) ?? throw AppException.NotFound($"Product variant {VariantId} not found.");
				var orderDetail = OrderDetail.Create(
					 VariantId,
					 Quantity,
					 variant.UnitPrice,
					 variant.Snapshot);

				orderDetails.Add(orderDetail);
			}

			return orderDetails;
		}
	}
}
