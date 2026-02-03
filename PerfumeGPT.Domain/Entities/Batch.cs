using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Batch : BaseEntity<Guid>, IHasCreatedAt
	{
		public Guid VariantId { get; set; }
		public Guid ImportDetailId { get; set; }
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int ImportQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public int ReservedQuantity { get; set; }
		public int AvailableInBatch => RemainingQuantity - ReservedQuantity;

		[Timestamp]
		public byte[] RowVersion { get; set; } = null!;
		// Navigation
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ImportDetail ImportDetail { get; set; } = null!;
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];

		// Audit
		public DateTime CreatedAt { get; set; }
	}
}
