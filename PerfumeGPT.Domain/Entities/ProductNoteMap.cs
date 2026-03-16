using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductNoteMap : BaseEntity<Guid>
	{
		public Guid ProductId { get; set; }
		public int ScentNoteId { get; set; }
		public NoteType NoteType { get; set; }

		// Navigation properties
		public virtual Product Product { get; set; } = null!;
		public virtual ScentNote ScentNote { get; set; } = null!;
	}
}
