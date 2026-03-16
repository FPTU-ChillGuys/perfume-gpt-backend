using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class CustomerAttributePreference : BaseEntity<Guid>
	{
		public Guid ProfileId { get; set; }
		public int AttributeValueId { get; set; }

		// Navigation properties
		public virtual CustomerProfile Profile { get; set; } = null!;
		public virtual AttributeValue AttributeValue { get; set; } = null!;
	}
}
