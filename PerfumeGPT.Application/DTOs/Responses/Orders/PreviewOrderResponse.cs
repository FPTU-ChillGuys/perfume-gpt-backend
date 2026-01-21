using PerfumeGPT.Application.DTOs.Responses.OrderDetails;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class PreviewOrderResponse
	{
		public List<OrderDetailListItems> Items { get; set; } = null!;
		public decimal SubTotal { get; set; }
		public decimal ShippingFee { get; set; }
		public decimal Discount { get; set; }
		public decimal Total { get; set; }
	}
}
