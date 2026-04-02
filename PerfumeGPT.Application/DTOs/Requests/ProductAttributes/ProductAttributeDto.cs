namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
	public record ProductAttributeDto
	{
		public int AttributeId { get; init; }
		public int ValueId { get; init; }
	}
}
