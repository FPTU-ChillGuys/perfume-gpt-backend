namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductLookupItem
	{
		public Guid Id { get; init; }
		public required string Name { get; init; }
		public required string BrandName { get; init; }
		public string? PrimaryImageUrl { get; init; }
	}
}
