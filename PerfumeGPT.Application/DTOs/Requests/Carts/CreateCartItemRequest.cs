namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public record CreateCartItemRequest
	{
		public Guid VariantId { get; init; }
		public int Quantity { get; init; }
	}
}
