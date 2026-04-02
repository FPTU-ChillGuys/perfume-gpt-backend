namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes
{
	public record UpdateScentNoteRequest
	{
		public required string Name { get; init; }
	}
}