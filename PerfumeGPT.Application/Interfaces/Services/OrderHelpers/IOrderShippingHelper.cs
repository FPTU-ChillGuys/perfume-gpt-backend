using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderShippingHelper
	{
		Task SetupShippingInfoAsync(Guid orderId, RecipientInformation? recipientRequest, Guid? customerId, Guid? savedAddressId);
		Task<bool> CreateGHNShippingOrderAsync(Order order, RecipientInfo recipientInfo);
		ShippingStatus? MapOrderStatusToShippingStatus(OrderStatus orderStatus);
	}
}
