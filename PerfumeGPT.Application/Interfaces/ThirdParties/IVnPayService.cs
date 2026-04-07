using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.VNPays;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IVnPayService
	{
		public Task<OrderCheckoutResponse> CreatePaymentUrlAsync(HttpContext context, VnPaymentRequest request);
		public VnPaymentResponse GetPaymentResponseAsync(IQueryCollection queryParameters);
		public Task<VnPayQueryResponse> QueryTransactionAsync(HttpContext context, VnPayQueryRequest request);
		public Task<VnPayRefundResponse> RefundAsync(HttpContext context, VnPayRefundRequest request);
	}
}
