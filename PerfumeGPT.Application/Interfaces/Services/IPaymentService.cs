using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.VNPays;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IPaymentService
	{
		Task<BaseResponse<VnPayReturnResponse>> GetVnPayReturnResponseAsync(IQueryCollection queryParameters);
		Task<BaseResponse<bool>> ProcessVnPayReturnAsync(IQueryCollection queryParameters);
		Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, bool isSuccess, string? failureReason = null);
		Task<BaseResponse<string>> ChangePaymentMethodAsync(Guid paymentId, PaymentInformation newMethod);
		Task<BaseResponse<string>> RetryPaymentWithMethodAsync(Guid paymentId, PaymentInformation? newMethod = null);
	}
}
