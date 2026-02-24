using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Attribute : BaseEntity<int>
	{
		public string Name { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public string InternalCode { get; set; } = string.Empty;
		public bool IsVariantLevel { get; set; }

		// Navigation properties
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<AttributeValue> AttributeValues { get; set; } = [];
	}
}
