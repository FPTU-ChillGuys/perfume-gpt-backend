namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class CreateVariantRequest : UpdateVariantRequest
	{
		public Guid ProductId { get; set; }
		
		// Upload First Pattern: Single image uploaded to temporary storage first
		public Guid? TemporaryMediaId { get; set; }
	}
}
