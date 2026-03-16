namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public class CreateCartItemRequest : UpdateCartItemRequest
	{
		public Guid VariantId { get; set; }
	}
}
