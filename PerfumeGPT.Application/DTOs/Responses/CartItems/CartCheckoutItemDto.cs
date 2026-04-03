namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public record CartCheckoutItemDto
	{
		public Guid VariantId { get; init; }
      public string VariantName { get; init; } = string.Empty;
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal SubTotal { get; init; }
		public decimal Discount { get; init; }
		public decimal FinalTotal { get; init; }
	}
}
