using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Category : BaseEntity<int>
	{
		public string Name { get; set; } = null!;

		// Navigation
		public virtual ICollection<Product> Products { get; set; } = [];
	}
}
