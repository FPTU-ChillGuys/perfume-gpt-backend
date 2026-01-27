using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services.OrderHelpers
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

		public async Task<BaseResponse<List<OrderDetail>>> CreateOrderDetailsAsync(List<(Guid VariantId, int Quantity)> items)
		{
			var stockValidation = await _inventoryManager.ValidateStockAvailabilityAsync(items);
			if (!stockValidation.Success)
			{
				return BaseResponse<List<OrderDetail>>.Fail(
					stockValidation.Message ?? "Stock validation failed.",
					stockValidation.ErrorType);
			}

			var orderDetails = new List<OrderDetail>();
			foreach (var item in items)
			{
				var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
				if (!variantResponse.Success || variantResponse.Payload == null)
				{
					return BaseResponse<List<OrderDetail>>.Fail(
						$"Product variant {item.VariantId} not found.",
						ResponseErrorType.NotFound);
				}

				var variant = variantResponse.Payload;
				var orderDetail = new OrderDetail
				{
					VariantId = item.VariantId,
					Quantity = item.Quantity,
					UnitPrice = variant.BasePrice,
					Snapshot = $"{variant.ProductName} - {variant.VolumeMl}ml - {variant.ConcentrationName} - {variant.Type}"
				};

				orderDetails.Add(orderDetail);
			}

			return BaseResponse<List<OrderDetail>>.Ok(orderDetails);
		}
	}
}
