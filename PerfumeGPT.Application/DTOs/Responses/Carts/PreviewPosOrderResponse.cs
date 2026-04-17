using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public class PreviewPosOrderResponse
	{
		public List<PosOrderDetailListItem> Items { get; set; } = [];
		public decimal SubTotal { get; set; }
		public decimal ShippingFee { get; set; }
		public decimal Discount { get; set; }
		public decimal TotalPrice { get; set; }
	}
}
