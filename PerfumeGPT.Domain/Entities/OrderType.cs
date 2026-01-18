using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderType : BaseEntity<int>
	{
		public string? Name { get; set; } // instore, online, shopee

		// Navigation
		public virtual ICollection<Order> Orders { get; set; } = [];
	}
}
