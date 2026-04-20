using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderShippingHelper
	{
		Task SetupShippingInfoAsync(Order order, ContactAddressInformation? contactAddressRequest, Guid? customerId, Guid? savedAddressId, decimal? shippingFee = null);
		Task<DateTime?> GetLeadTimeAsync(int districtId, string wardCode);
		Task<bool> CreateGHNShippingOrderAsync(Order order, ContactAddress contactAddress);
		Task<bool> CreateGHNShippingOrderAsync(OrderReturnRequest returnRequest, ContactAddress contactAddress);
	}
}
