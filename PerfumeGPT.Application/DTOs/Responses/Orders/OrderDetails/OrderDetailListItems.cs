namespace PerfumeGPT.Application.DTOs.Responses.Orders.OrderDetails
{
	public record OrderDetailListItems
	{
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public required string ImageUrl { get; init; }
		public int Quantity { get; init; }
		public int Total { get; init; }
	}
}
