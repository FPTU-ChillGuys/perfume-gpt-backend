namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public record CartCheckoutItemDto
	{
		public Guid VariantId { get; init; }
		public Guid? BatchId { get; init; }
		public string? BatchCode { get; init; }
		public required string VariantName { get; init; }
		public string? ImageUrl { get; init; }
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal SubTotal { get; init; }
		public decimal Discount { get; init; }
		public decimal FinalTotal { get; init; }

		public Guid? AppliedPromotionItemId { get; init; }
		public int DiscountedQuantity { get; init; }

		public decimal ApportionedVoucherDiscount { get; init; }
	}
}
