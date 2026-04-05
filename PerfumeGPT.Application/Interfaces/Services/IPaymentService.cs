using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.DTOs.Responses.VNPays;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IPaymentService
	{
		Task<VnPayReturnResponse> ProcessVnPayReturnAsync(IQueryCollection queryParameters);
		Task<MomoReturnResponse> ProcessMomoReturnAsync(IQueryCollection queryParameters);
		Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, ConfirmPaymentRequest request);
		Task<BaseResponse<string>> ChangePaymentMethodAsync(Guid paymentId, PaymentInformation newMethod);
		Task<BaseResponse<string>> RetryPaymentWithMethodAsync(Guid paymentId, PaymentInformation? newMethod = null);
		Task<BaseResponse<PaymentTransactionOverviewResponse>> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request);
	}
}
