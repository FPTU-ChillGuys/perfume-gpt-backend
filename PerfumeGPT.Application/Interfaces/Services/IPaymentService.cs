using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.DTOs.Responses.PayOs;
using PerfumeGPT.Application.DTOs.Responses.VNPays;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IPaymentService
	{
		Task<VnPayReturnResponse> ProcessVnPayReturnAsync(IQueryCollection queryParameters);
		Task<MomoReturnResponse> ProcessMomoReturnAsync(IQueryCollection queryParameters);
		Task<PayOsReturnResponse> ProcessPayOsReturnAsync(IQueryCollection queryParameters, bool isCancelCallback = false);
		Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, ConfirmPaymentRequest request);
		Task<BaseResponse<string>> RetryOrChangePaymentMethodAsync(Guid paymentId, RetryOrChangePaymentRequest newMethod);
		Task<BaseResponse<PaymentTransactionOverviewResponse>> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request);
	}
}
