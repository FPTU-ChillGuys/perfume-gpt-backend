using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class StockReservation : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid OrderId { get; set; }
		public Guid BatchId { get; set; }
		public Guid VariantId { get; set; }
		public int ReservedQuantity { get; set; }
		public ReservationStatus Status { get; set; }
		public DateTime? ExpiresAt { get; set; }

		// Navigation
		public virtual Order Order { get; set; } = null!;
		public virtual Batch Batch { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;

		// Audit
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
