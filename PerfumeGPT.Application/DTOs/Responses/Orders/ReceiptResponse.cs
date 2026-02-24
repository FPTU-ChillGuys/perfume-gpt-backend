namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class ReceiptResponse
	{
		public Guid OrderId { get; set; }
		public DateTime OrderDate { get; set; }
		public string OrderStatus { get; set; } = null!;
		public string StaffName { get; set; } = null!;
		public string CustomerName { get; set; } = null!;
		public string RecipientPhone { get; set; } = null!;
		public string RecipientAddress { get; set; } = null!;
		public List<ReceiptItemDto> Items { get; set; } = null!;
		public decimal Subtotal { get; set; }
		public decimal Discount { get; set; }
		public decimal Tax { get; set; }
		public decimal Total { get; set; }
		public string PaymentMethod { get; set; } = null!;
		public string? Note { get; set; }
	}

	public class ReceiptItemDto
	{
		public string ProductName { get; set; } = null!;
		public string VariantInfo { get; set; } = null!; // e.g., "50ml Eau de Parfum"
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Subtotal { get; set; }
	}
}
