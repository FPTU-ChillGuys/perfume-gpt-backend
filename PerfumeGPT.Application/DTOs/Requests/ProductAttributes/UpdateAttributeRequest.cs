namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
	public class UpdateAttributeRequest
	{
		public string InternalCode { get; set; } = null!;
		public string? Name { get; set; }
		public string? Description { get; set; }
		public bool? IsVariantLevel { get; set; }
	}
}
