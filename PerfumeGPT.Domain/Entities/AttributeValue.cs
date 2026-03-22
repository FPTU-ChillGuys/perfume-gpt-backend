using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class AttributeValue : BaseEntity<int>
	{
		public int AttributeId { get; set; }
		public string Value { get; set; } = string.Empty;

		// Navigation properties
		public virtual Attribute Attribute { get; set; } = null!;
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<CustomerAttributePreference> CustomerAttributePreferences { get; set; } = [];

		// Business logic methods
		public static void EnsureCanBeDeleted(bool isInUse)
		{
			if (isInUse)
				throw DomainException.BadRequest("Attribute Value is in use and cannot be deleted.");
		}
	}
}
