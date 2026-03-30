namespace PerfumeGPT.Application.DTOs.Requests.Payments
{
	public class ConfirmPaymentRequest
	{
		public bool IsSuccess { get; set; }
		public string? failureReason { get; set; }
	}
}
