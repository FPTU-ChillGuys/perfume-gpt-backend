using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes
{
	public record ScentNoteResponse
	{
		public int Id { get; init; }
		public required string Name { get; init; }
	}
}
