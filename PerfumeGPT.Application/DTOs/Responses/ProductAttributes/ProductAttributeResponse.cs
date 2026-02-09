namespace PerfumeGPT.Application.DTOs.Responses.ProductAttributes
{
	public class ProductAttributeResponse
	{
		public Guid Id { get; set; }
		public int AttributeId { get; set; }
		public int ValueId { get; set; }
		public string Attribute { get; set; } = null!;
		public string Description { get; set; } = string.Empty;
		public string Value { get; set; } = null!;
	}
}
