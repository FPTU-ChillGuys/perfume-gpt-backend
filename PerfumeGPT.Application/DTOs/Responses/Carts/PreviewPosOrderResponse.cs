using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record PreviewPosOrderResponse
	{
		public List<PosOrderDetailListItem> Items { get; init; } = [];
		public decimal SubTotal { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { get; init; }
		public decimal TotalPrice { get; init; }
		public decimal RequiredDepositAmount { get; init; }
	}
}
