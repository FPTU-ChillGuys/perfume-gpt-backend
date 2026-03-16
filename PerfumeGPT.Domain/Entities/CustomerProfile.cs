using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerProfile : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid UserId { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public decimal? MinBudget { get; set; }
		public decimal? MaxBudget { get; set; }

		// Navigation
		public virtual User User { get; set; } = null!;
		public virtual ICollection<CustomerNotePreference> NotePreferences { get; set; } = [];
		public virtual ICollection<CustomerFamilyPreference> FamilyPreferences { get; set; } = [];
		public virtual ICollection<CustomerAttributePreference> AttributePreferences { get; set; } = [];

		// Audit
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
