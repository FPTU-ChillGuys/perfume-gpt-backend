using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class AttributeValue : BaseEntity<int>
	{
		public int AttributeId { get; set; }
		public string Value { get; set; } = string.Empty;

		// Navigation properties
		public virtual Attribute Attribute { get; set; } = null!;
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
	}
}
