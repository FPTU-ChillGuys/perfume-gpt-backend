namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands
{
	public record UpdateBrandRequest
	{
		public required string Name { get; init; }
	}
}
