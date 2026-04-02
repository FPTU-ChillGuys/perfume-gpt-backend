namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories
{
	public record CategoryResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}