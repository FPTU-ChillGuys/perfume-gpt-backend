using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record GetCartItemsResponse
	{
		public required List<GetCartItemResponse> Items { get; init; }
	}
}
