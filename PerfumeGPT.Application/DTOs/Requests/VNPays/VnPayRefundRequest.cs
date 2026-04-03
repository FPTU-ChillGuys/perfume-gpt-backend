namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
	public record VnPayRefundRequest
	{
		public Guid OrderId { get; init; }
		public decimal Amount { get; init; }
		public Guid PaymentId { get; init; }
		public string TransactionType { get; init; } = "02"; // 02 for full refund, 03 for partial
		public string? TransactionNo { get; init; }
		public string CreateBy { get; init; } = string.Empty;
		public string OrderInfo { get; init; } = string.Empty;
		public string TransactionDate { get; init; } = string.Empty;
	}
}
