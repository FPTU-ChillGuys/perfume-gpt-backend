namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public class GetCartItemResponse
	{
		public Guid CartItemId { get; set; }
		public Guid VariantId { get; set; }
		public string? ImageUrl { get; set; }
		public string VariantName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public decimal VariantPrice { get; set; }
		public int Quantity { get; set; }
		public decimal SubTotal => VariantPrice * Quantity;
	}
}
