namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes
{
	public record ScentNoteLookupResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}
