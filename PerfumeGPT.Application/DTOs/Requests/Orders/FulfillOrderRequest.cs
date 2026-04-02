namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record FulfillOrderRequest
	{
		public required List<FulfillOrderItemRequest> Items { get; init; }
	}

	public record FulfillOrderItemRequest
	{
		public Guid OrderDetailId { get; init; }
		public required string ScannedBatchCode { get; init; }
		public int Quantity { get; init; }
	}
}
