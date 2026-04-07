namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
	public record VnPayQueryRequest
	{
		public Guid PaymentId { get; init; }
		public string OrderInfo { get; init; } = string.Empty;
		public string TransactionDate { get; init; } = string.Empty;
		public string? TransactionNo { get; init; }
	}
}
