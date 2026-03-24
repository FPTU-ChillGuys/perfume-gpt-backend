using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes
{
	public class UpdateScentNoteRequest
	{
		[Required]
		public string Name { get; set; } = null!;
	}
}