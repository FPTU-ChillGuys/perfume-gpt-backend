namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record ReceiptResponse
	{
		public Guid OrderId { get; init; }
		public required string Code { get; init; }
		public DateTime OrderDate { get; init; }
		public required string OrderStatus { get; init; }
		public required string StaffName { get; init; }
		public required string CustomerName { get; init; }
		public required string RecipientPhone { get; init; }
		public required string RecipientAddress { get; init; }
		public required List<ReceiptItemDto> Items { get; init; }
		public decimal Subtotal { get; init; }
		public decimal DepositeAmount { get; init; }
		public decimal RemainingAmount { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { get; init; }
		public decimal Tax { get; init; }
		public decimal Total { get; init; }
		public required string PaymentMethod { get; init; }
		public string? Note { get; init; }
	}

	public record ReceiptItemDto
	{
		public required string ProductName { get; init; }
		public required string VariantInfo { get; init; } // e.g., "50ml Eau de Parfum"
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal Subtotal { get; init; }
	}
}
