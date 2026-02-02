namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class CreateVariantRequest : UpdateVariantRequest
	{
		public Guid ProductId { get; set; }

		// Upload First Pattern: Multiple images uploaded to temporary storage first
		public List<Guid>? TemporaryMediaIds { get; set; }
	}
}

