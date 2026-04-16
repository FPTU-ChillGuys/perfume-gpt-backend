namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
	public record VnPaymentResponse
	{
		public bool IsSuccess { get; init; }
		public required string Message { get; init; }

		// main response fields
		public Guid PaymentId { get; init; }
		public string? ResponseCode { get; init; }
		public string? PaymentInfo { get; init; }
		public string? OrderCode { get; init; }
		public string? PosSessionId { get; init; }
		public string? TransactionNo { get; init; }
		public decimal Amount { get; init; }
	}
}
