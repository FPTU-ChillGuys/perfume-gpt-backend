using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record CartCheckoutResponse
	{
		public required List<CartCheckoutItemDto> Items { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal TotalPrice { get; init; }
	}
}
