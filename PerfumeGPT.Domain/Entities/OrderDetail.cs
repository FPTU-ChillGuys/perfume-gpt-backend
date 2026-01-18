using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderDetail : BaseEntity<Guid>
	{
		public Guid OrderId { get; set; }
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public string Snapshot { get; set; } = null!; // name, volume, concentration, type

		// Navigation
		public virtual Order Order { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
	}
}
