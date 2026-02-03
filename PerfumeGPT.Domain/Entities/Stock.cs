using PerfumeGPT.Domain.Commons;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Stock : BaseEntity<Guid>
	{
		public Guid VariantId { get; set; }
		public int TotalQuantity { get; set; }
		public int ReservedQuantity { get; set; }
		public int AvailableQuantity => TotalQuantity - ReservedQuantity;
		public int LowStockThreshold { get; set; }

		[Timestamp]
		public byte[] RowVersion { get; set; } = null!;

		// Navigation
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ICollection<Notification> Notifications { get; set; } = [];
	}
}
