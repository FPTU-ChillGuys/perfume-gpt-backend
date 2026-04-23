using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Payments
{
	public record RetryOrChangePaymentRequest
	{
		public PaymentMethod? NewPaymentMethod { get; init; }
		public PaymentMethod? NewDepositMethod { get; init; }
		public string? PosSessionId { get; init; }
	}
}
