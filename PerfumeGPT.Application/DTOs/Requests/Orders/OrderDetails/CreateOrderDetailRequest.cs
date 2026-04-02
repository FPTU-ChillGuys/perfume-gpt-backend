namespace PerfumeGPT.Application.DTOs.Requests.Orders.OrderDetails
{
	public record CreateOrderDetailRequest
	{
		public Guid VariantId { get; init; }
		public int Quantity { get; init; }
	}
}
