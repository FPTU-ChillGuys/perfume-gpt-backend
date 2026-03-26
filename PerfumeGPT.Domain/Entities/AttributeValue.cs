using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class AttributeValue : BaseEntity<int>
	{
		private AttributeValue() { }

		public int AttributeId { get; private set; }
		public string Value { get; private set; } = string.Empty;

		// Navigation properties
		public virtual Attribute Attribute { get; set; } = null!;
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<CustomerAttributePreference> CustomerAttributePreferences { get; set; } = [];

		// Factory methods
		public static AttributeValue Create(int attributeId, string value)
		{
			if (string.IsNullOrWhiteSpace(value))
				throw DomainException.BadRequest("Attribute value cannot be empty.");

			return new AttributeValue
			{
				AttributeId = attributeId,
				Value = value.Trim()
			};
		}

		public void Update(string? value)
		{
			if (value == null) return;

			if (string.IsNullOrWhiteSpace(value))
				throw DomainException.BadRequest("Attribute value cannot be empty.");

			Value = value.Trim();
		}

		// Business logic methods
		public static void EnsureCanBeDeleted(bool isInUse)
		{
			if (isInUse)
				throw DomainException.BadRequest("Attribute value is in use and cannot be deleted.");
		}
	}
}
