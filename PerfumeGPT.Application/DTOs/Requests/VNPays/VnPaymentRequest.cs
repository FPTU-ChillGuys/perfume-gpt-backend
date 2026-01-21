namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
	public class VnPaymentRequest
	{
		public Guid OrderId { get; set; }
		public Guid PaymentId { get; set; }
		public int Amount { get; set; } = 0;
	}
}
