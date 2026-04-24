using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Payments
{
	public record CreatePickupPaymentRequest
	{
		public PaymentMethod PaymentMethod { get; set; }
		public string? PosSessionId { get; set; }
	}
}
