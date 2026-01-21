namespace PerfumeGPT.Application.DTOs.Responses.OrderDetails
{
	public class OrderDetailListItems
	{
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public string ImageUrl { get; set; } = null!;
		public int Quantity { get; set; }
		public int Total { get; set; }
	}
}
