namespace PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes
{
	public record AttributeLookupItem
	{
		public int Id { get; init; }
		public required string InternalCode { get; init; }
		public required string Name { get; init; }
		public string? Description { get; init; }
		public bool IsVariantLevel { get; init; }
	}
}
