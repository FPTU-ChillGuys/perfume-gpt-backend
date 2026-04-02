namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories
{
	public record UpdateCategoryRequest
	{
		public required string Name { get; init; }
	}
}