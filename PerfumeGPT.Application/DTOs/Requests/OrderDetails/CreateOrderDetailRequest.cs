namespace PerfumeGPT.Application.DTOs.Requests.OrderDetails
{
	public class CreateOrderDetailRequest
	{
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }
	}
}
