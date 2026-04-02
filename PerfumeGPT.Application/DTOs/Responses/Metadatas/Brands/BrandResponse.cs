namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands
{
	public record BrandResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}
