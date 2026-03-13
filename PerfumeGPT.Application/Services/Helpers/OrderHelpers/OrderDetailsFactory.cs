using PerfumeGPT.Application.DTOs.Responses.Base;
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
				var variant = await _variantService.GetVariantForCreateOrderAsync(item.VariantId);
				if (variant == null)
				{
					return BaseResponse<List<OrderDetail>>.Fail(
						$"Product variant {item.VariantId} not found.",
						ResponseErrorType.NotFound);
				}

				var orderDetail = new OrderDetail
				{
					VariantId = item.VariantId,
					Quantity = item.Quantity,
					UnitPrice = variant.UnitPrice,
					Snapshot = variant.Snapshot,
				};

				orderDetails.Add(orderDetail);
			}

			return BaseResponse<List<OrderDetail>>.Ok(orderDetails);
		}
	}
}
