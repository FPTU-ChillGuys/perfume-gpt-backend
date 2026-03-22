namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public class CartItemPriceDto
	{
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = string.Empty;
		public decimal VariantPrice { get; set; }
		public int Quantity { get; set; }
		public decimal SubTotal => VariantPrice * Quantity;
	}
}
