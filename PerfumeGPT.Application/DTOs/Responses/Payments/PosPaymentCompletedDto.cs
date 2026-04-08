namespace PerfumeGPT.Application.DTOs.Responses.Payments
{
	public record PosPaymentCompletedDto
	{
		public required Guid OrderId { get; init; }
		public required string Status { get; init; }
		public required string Message { get; init; }
	}
}
