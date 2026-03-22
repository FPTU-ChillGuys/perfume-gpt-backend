namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
	public class UpdateAttributeRequest
	{
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
		public bool IsVariantLevel { get; set; }
	}
}
