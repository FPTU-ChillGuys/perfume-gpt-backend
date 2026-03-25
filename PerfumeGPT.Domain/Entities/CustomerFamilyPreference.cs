using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerFamilyPreference : BaseEntity<Guid>
	{
		protected CustomerFamilyPreference() { }

		public Guid ProfileId { get; private set; }
		public int FamilyId { get; private set; }

		// Navigation properties
		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual OlfactoryFamily Family { get; set; } = null!;

		// Factory methods
		public static CustomerFamilyPreference Create(Guid profileId, int familyId)
		{
			if (profileId == Guid.Empty)
			{
				throw DomainException.BadRequest("Profile ID is required.");
			}

			if (familyId <= 0)
			{
				throw DomainException.BadRequest("Family ID must be greater than 0.");
			}

			return new CustomerFamilyPreference
			{
				ProfileId = profileId,
				FamilyId = familyId
			};
		}
	}
}
