using PerfumeGPT.Application.DTOs.Requests.PayOs;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.PayOs;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IPayOsService
	{
		Task<OrderCheckoutResponse> CreatePaymentUrlAsync(PayOsPaymentRequest request);
		Task<PayOsPaymentInfoResponse> GetPaymentInfoAsync(string orderCode, Guid paymentId);
	}
}
