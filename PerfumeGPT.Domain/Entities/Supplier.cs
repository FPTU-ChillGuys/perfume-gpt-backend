using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Supplier : BaseEntity<int>
	{
		public string Name { get; set; } = null!;
		public string ContactEmail { get; set; } = null!;
		public string Phone { get; set; } = null!;
		public string Address { get; set; } = null!;

		// Navigation
		public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
	}
}
