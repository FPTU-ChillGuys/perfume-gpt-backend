using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class StockAdjustmentDetail : BaseEntity<Guid>
	{
		public Guid StockAdjustmentId { get; set; }
		public Guid ProductVariantId { get; set; }
		public Guid BatchId { get; set; }
		public int AdjustmentQuantity { get; set; }
		public int ApprovedQuantity { get; set; }
		public string? Note { get; set; }

		// Navigation
		public virtual StockAdjustment StockAdjustment { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Batch Batch { get; set; } = null!;
	}
}
