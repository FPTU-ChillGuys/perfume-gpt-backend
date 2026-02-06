namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class FulfillOrderRequest
	{
		public List<FulfillOrderItemRequest> Items { get; set; } = [];
	}

	public class FulfillOrderItemRequest
	{
		public Guid OrderDetailId { get; set; }
		public string ScannedBatchCode { get; set; } = null!;
		public int Quantity { get; set; }
	}
}
