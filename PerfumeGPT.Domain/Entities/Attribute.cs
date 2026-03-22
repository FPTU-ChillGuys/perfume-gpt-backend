using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

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

		// Business logic methods
		public static void EnsureCanBeDeleted(bool isInUse)
		{
			if (isInUse)
				throw DomainException.BadRequest("Attribute is in use and cannot be deleted.");
		}
	}
}
