namespace PerfumeGPT.Application.DTOs.Responses.Payments
{
	public record PosPaymentLinkDto
	{
		public required Guid OrderId { get; init; }
		public required Guid PaymentId { get; init; }
		public required string Method { get; init; }
		public required string PaymentUrl { get; init; }
	}
}
