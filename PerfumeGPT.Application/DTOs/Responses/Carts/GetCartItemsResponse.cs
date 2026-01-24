using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public class GetCartItemsResponse
	{
		public List<GetCartItemResponse> Items { get; set; } = [];
	}
}
