using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class CartItem : BaseEntity<Guid>
	{
		public Guid UserId { get; set; }
		public Guid VariantId { get; set; }
		public int Quantity { get; set; }

		// Navigation
		public virtual User User { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
	}
}
