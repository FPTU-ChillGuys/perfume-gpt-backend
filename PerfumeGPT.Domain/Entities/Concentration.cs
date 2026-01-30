using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Concentration : BaseEntity<int>
	{
		public string Name { get; set; } = null!;

		// Navigation
		public virtual ICollection<ProductVariant> Variants { get; set; } = [];
	}
}
