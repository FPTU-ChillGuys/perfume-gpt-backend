namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
	public class VnPaymentResponse
	{
		public bool IsSuccess { get; set; }
		public string Message { get; set; } = null!;

		// main response fields
		public Guid PaymentId { get; set; }
		public string ResponseCode { get; set; } = null!;
		public string PaymentInfo { get; set; } = null!;
		public string TransactionNo { get; set; } = null!;
		public decimal Amount { get; set; }
	}
}
