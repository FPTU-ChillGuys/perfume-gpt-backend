using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Attribute : BaseEntity<int>
	{
		public string InternalCode { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
		public bool IsVariantLevel { get; set; }

		// Navigation properties
		public virtual ICollection<ProductAttribute> ProductAttributes { get; set; } = [];
		public virtual ICollection<AttributeValue> AttributeValues { get; set; } = [];
	}
}
