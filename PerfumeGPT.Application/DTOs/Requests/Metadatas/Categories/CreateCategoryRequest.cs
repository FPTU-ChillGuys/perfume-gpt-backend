namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories
{
	public record CreateCategoryRequest
	{
		public required string Name { get; init; }
	}
}