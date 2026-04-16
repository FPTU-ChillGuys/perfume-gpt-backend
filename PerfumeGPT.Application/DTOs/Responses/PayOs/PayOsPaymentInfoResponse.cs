namespace PerfumeGPT.Application.DTOs.Responses.PayOs
{
	public record PayOsPaymentInfoResponse
	{
		public bool IsSuccess { get; init; }
		public bool IsPaid { get; init; }
		public long OrderCode { get; init; }
        public string? ExtractedOrderCode { get; init; }
		public string? PosSessionId { get; init; }
		public decimal Amount { get; init; }
		public string? Status { get; init; }
		public string? PaymentLinkId { get; init; }
		public string? Message { get; init; }
	}
}
