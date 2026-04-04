using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IMomoService
	{
		Task<OrderCheckoutResponse> CreatePaymentUrlAsync(HttpContext context, MomoPaymentRequest request);
		MomoPaymentResponse GetPaymentResponseAsync(IQueryCollection queryParameters);
		Task<MomoRefundResponse> RefundAsync(HttpContext context, MomoRefundRequest request);
	}
}
