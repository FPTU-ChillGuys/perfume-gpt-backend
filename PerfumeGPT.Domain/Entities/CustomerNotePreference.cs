using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerNotePreference : BaseEntity<Guid>
	{
		protected CustomerNotePreference() { }

		public Guid ProfileId { get; private set; }
		public int NoteId { get; private set; }
		public NoteType NoteType { get; private set; }

		// Navigation properties
		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual ScentNote ScentNote { get; set; } = null!;

		// Factory methods
		public static CustomerNotePreference Create(Guid profileId, int noteId, NoteType noteType)
		{
			if (profileId == Guid.Empty)
			{
				throw DomainException.BadRequest("Profile ID là bắt buộc và không được để trống.");
			}

			if (noteId <= 0)
			{
				throw DomainException.BadRequest("Note ID phải lớn hơn 0.");
			}

			return new CustomerNotePreference
			{
				ProfileId = profileId,
				NoteId = noteId,
				NoteType = noteType
			};
		}
	}
}
