namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
	public class PosOrderDetailListItem
	{
		public Guid VariantId { get; set; }
		public Guid BatchId { get; set; }
		public string VariantName { get; set; } = null!;
		public string BatchCode { get; set; } = null!;
		public string ImageUrl { get; set; } = string.Empty;
		public int Quantity { get; set; }

		public decimal UnitPrice { get; set; }
		public decimal SubTotal { get; set; }
		public decimal Discount { get; set; }
		public decimal FinalTotal { get; set; }
	}
}
