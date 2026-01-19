namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class CreateVariantRequest : UpdateVariantRequest
	{
		public Guid ProductId { get; set; }
	}
}
