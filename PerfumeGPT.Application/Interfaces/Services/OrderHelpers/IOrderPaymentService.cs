using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderPaymentService
	{
		Task<string> CreatePaymentAndGenerateResponseAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod);
	}
}
