using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderShippingHelper
	{
		Task<BaseResponse<decimal>> SetupShippingInfoAsync(
			Guid orderId,
			RecipientInformation? recipientRequest,
			Guid? customerId,
			decimal? preCalculatedShippingFee = null,
			Order? orderToUpdate = null);

		Task<BaseResponse<string>> CreateGHNShippingOrderAsync(
			Order order,
			RecipientInfo recipientInfo);

		ShippingStatus? MapOrderStatusToShippingStatus(OrderStatus orderStatus);
	}
}
