using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductAttribute : BaseEntity<Guid>
	{
		public Guid? ProductId { get; set; }
		public Guid? VariantId { get; set; }
		public int AttributeId { get; set; }
		public int ValueId { get; set; }

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ProductVariant? Variant { get; set; }
		public virtual Attribute Attribute { get; set; } = null!;
		public virtual AttributeValue Value { get; set; } = null!;
	}
}
