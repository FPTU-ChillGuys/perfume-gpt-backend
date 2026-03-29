using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductNoteMap : BaseEntity<Guid>
	{
		protected ProductNoteMap() { }

		public Guid ProductId { get; private set; }
		public int ScentNoteId { get; private set; }
		public NoteType NoteType { get; private set; }

		// Navigation properties
		public virtual Product Product { get; set; } = null!;
		public virtual ScentNote ScentNote { get; set; } = null!;

		// Factory methods
		public static ProductNoteMap Create(int scentNoteId, NoteType noteType)
		{
			if (scentNoteId <= 0)
				throw DomainException.BadRequest("Scent note ID must be greater than 0.");

			return new ProductNoteMap
			{
				ScentNoteId = scentNoteId,
				NoteType = noteType
			};
		}
	}
}
