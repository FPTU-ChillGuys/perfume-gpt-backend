namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public class CartItemPriceDto
	{
		public decimal VariantPrice { get; set; }
		public int Quantity { get; set; }
		public decimal SubTotal => VariantPrice * Quantity;
	}
}
