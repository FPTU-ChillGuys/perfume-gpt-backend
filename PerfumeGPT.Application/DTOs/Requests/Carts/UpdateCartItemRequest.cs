namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public record UpdateCartItemRequest
	{
		public int Quantity { get; init; }
	}
}
