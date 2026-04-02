using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes
{
	public record ScentNoteDto
	{
		public int NoteId { get; init; }
		public NoteType Type { get; init; }
	}
}
