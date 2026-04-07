namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
	public record VnPayQueryResponse
	{
		public bool IsSuccess { get; init; }
		public required string Message { get; init; }
		public Guid PaymentId { get; init; }
		public string? ResponseCode { get; init; }
		public string? TransactionStatus { get; init; }
		public string? TransactionNo { get; init; }
		public decimal Amount { get; init; }
	}
}
