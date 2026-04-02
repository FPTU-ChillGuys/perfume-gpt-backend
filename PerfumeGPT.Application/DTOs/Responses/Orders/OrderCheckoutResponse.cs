namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record OrderCheckoutResponse
	{
		public required string PaymentUrl { get; init; }
		public Guid OrderId { get; init; }
	}
}
