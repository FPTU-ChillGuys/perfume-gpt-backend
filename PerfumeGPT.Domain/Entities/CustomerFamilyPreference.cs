using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerFamilyPreference : BaseEntity<Guid>
	{
		public Guid ProfileId { get; set; }
		public int FamilyId { get; set; }

		// Navigation properties
		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual OlfactoryFamily Family { get; set; } = null!;
	}
}
