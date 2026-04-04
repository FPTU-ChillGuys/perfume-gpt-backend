namespace PerfumeGPT.Application.DTOs.Requests.Momos
{
	public record MomoRefundRequest
	{
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public decimal Amount { get; init; }
		public Guid PaymentId { get; init; }
		public string? TransactionNo { get; init; }
		public string Description { get; init; } = string.Empty;
	}
}
