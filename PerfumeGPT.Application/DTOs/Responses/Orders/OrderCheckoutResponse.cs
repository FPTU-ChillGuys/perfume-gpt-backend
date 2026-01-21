namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class OrderCheckoutResponse
	{
		public string PaymentUrl { get; set; } = string.Empty;
		public Guid OrderId { get; set; }
	}
}
