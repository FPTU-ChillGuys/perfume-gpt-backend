namespace PerfumeGPT.Application.DTOs.Requests.Momos
{
	public record MomoPaymentRequest
	{
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public Guid PaymentId { get; init; }
		public int Amount { get; init; }
		public string? PosSessionId { get; init; }
	}
}
