using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public class GetCartResponse
	{
		public List<GetCartItemResponse> Items { get; set; } = [];
		public decimal ShippingFee { get; set; }
		public decimal TotalPrice { get; set; }
	}
}
