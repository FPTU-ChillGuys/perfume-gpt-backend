using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerNotePreference : BaseEntity<Guid>
	{
		public Guid ProfileId { get; set; }
		public int NoteId { get; set; }

		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual ScentNote ScentNote { get; set; } = null!;
	}
}
