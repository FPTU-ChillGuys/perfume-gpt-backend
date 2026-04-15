namespace PerfumeGPT.Application.DTOs.Requests.Payments
{
	public record ConfirmPaymentRequest
	{
		public bool IsSuccess { get; init; }
		public string? FailureReason { get; init; }
       public string? PosSessionId { get; init; }
	}
}
