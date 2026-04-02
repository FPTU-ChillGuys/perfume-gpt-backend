namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes
{
	public record CreateScentNoteRequest
	{
		public required string Name { get; init; }
	}
}
