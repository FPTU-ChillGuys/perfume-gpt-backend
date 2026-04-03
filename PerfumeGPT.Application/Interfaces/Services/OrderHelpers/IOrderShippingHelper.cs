using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderShippingHelper
	{
		Task SetupShippingInfoAsync(Order order, ContactAddressInformation? contactAddressRequest, Guid? customerId, Guid? savedAddressId);
		Task<bool> CreateGHNShippingOrderAsync(Order order, ContactAddress contactAddress);
		Task<bool> CreateGHNShippingOrderAsync(OrderReturnRequest returnRequest, ContactAddress contactAddress);
		ShippingStatus? MapOrderStatusToShippingStatus(OrderStatus orderStatus);
	}
}
