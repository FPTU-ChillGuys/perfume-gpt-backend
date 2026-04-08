namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
	public record VnPaymentRequest
	{
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public Guid PaymentId { get; init; }
		public int Amount { get; init; } = 0;
		public string? PosSessionId { get; init; }
	}
}
