using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderPaymentService
	{
		Task<CreatePaymentResponseDto> CreatePaymentAndGenerateResponseAsync(Order order, PaymentMethod paymentMethod, string? posSessionId);
	}
}
