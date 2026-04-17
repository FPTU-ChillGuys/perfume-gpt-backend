namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
	public record VnPayRefundRequest
	{
		public Guid OrderId { get; init; }
		public decimal Amount { get; init; }
		public Guid PaymentId { get; init; }
		public required string TransactionType { get; init; }
		public string? TransactionNo { get; init; }
		public string CreateBy { get; init; } = string.Empty;
		public string OrderInfo { get; init; } = string.Empty;
		public string TransactionDate { get; init; } = string.Empty;
	}
}
