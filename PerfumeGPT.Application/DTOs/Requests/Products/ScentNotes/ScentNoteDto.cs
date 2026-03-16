using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products.ScentNotes
{
	public class ScentNoteDto
	{
		public int NoteId { get; set; }
		public NoteType Type { get; set; }
	}
}
