namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories
{
	public record CategoriesLookupItem
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}
