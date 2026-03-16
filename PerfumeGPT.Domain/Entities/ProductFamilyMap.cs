using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductFamilyMap : BaseEntity<Guid>
	{
		public Guid ProductId { get; set; }
		public int OlfactoryFamilyId { get; set; }

		// Navigation properties
		public virtual Product Product { get; set; } = null!;
		public virtual OlfactoryFamily OlfactoryFamily { get; set; } = null!;
	}
}
