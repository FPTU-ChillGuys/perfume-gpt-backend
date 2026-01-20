namespace PerfumeGPT.Application.DTOs.Requests.CartItems
{
	public class CreateCartItemRequest : UpdateCartItemRequest
	{

		public Guid VariantId { get; set; }
	}
}
