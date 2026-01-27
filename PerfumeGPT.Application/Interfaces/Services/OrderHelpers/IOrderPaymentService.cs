using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderPaymentService
	{
		Task<BaseResponse<string>> CreatePaymentAndGenerateResponseAsync(
			Guid orderId,
			decimal amount,
			PaymentMethod paymentMethod,
			string successMessage);
	}
}
