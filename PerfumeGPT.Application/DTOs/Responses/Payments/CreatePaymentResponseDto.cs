namespace PerfumeGPT.Application.DTOs.Responses.Payments
{
	public record CreatePaymentResponseDto
	{
		public Guid? PaymentId { get; init; }
		public string? PaymentUrl { get; init; }
		public Guid? OrderId { get; init; }
	}
}
