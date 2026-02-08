using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.CartItems
{
	public class CreateCartItemRequest : UpdateCartItemRequest
	{
		[JsonIgnore]
		public Guid CartId { get; set; }

		public Guid VariantId { get; set; }
	}
}
