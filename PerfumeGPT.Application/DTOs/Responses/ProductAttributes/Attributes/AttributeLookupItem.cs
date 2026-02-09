namespace PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes
{
	public class AttributeLookupItem
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public bool IsVariantLevel { get; set; }
	}
}
