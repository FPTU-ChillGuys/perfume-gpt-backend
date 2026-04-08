using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderPaymentService
	{
		Task<string> CreatePaymentAndGenerateResponseAsync(Order order, decimal amount, PaymentMethod paymentMethod, string? posSessionId);
	}
}
