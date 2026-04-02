namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies
{
	public record OlfactoryLookupResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}
