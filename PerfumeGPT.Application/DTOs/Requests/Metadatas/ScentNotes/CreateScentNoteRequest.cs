using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes
{
	public class CreateScentNoteRequest
	{
		[Required]
		public string Name { get; set; } = null!;
	}
}
