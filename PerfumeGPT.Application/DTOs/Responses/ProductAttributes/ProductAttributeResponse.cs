namespace PerfumeGPT.Application.DTOs.Responses.ProductAttributes
{
	public record ProductAttributeResponse
	{
		public Guid Id { get; init; }
		public int AttributeId { get; init; }
		public int ValueId { get; init; }
		public required string Attribute { get; init; }
		public string? Description { get; init; }
		public required string Value { get; init; }
	}
}
