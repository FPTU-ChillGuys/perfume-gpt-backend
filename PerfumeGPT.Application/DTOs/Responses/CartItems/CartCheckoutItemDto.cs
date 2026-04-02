namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public record CartCheckoutItemDto
	{
		public Guid VariantId { get; init; }
		public int Quantity { get; init; }
	}
}
