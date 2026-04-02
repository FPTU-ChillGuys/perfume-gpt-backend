namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands
{
	public record CreateBrandRequest
	{
		public required string Name { get; init; }
	}
}
