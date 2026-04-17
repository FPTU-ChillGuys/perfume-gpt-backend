using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerAttributePreference : BaseEntity<Guid>
	{
		protected CustomerAttributePreference() { }

		public Guid ProfileId { get; private set; }
		public int AttributeValueId { get; private set; }

		// Navigation properties
		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual AttributeValue AttributeValue { get; set; } = null!;

		// Factory methods
		public static CustomerAttributePreference Create(Guid profileId, int attributeValueId)
		{
			if (profileId == Guid.Empty)
			{
				throw DomainException.BadRequest("Profile ID là bắt buộc.");
			}

			if (attributeValueId <= 0)
			{
				throw DomainException.BadRequest("Attribute value ID phải lớn hơn 0.");
			}

			return new CustomerAttributePreference
			{
				ProfileId = profileId,
				AttributeValueId = attributeValueId
			};
		}
	}
}
