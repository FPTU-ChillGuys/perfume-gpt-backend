namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public record CartItemPriceDto
	{
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public decimal VariantPrice { get; init; }
		public int Quantity { get; init; }
		public decimal SubTotal => VariantPrice * Quantity;
	}
}
