using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class StockAdjustmentDetail : BaseEntity<Guid>
	{
		protected StockAdjustmentDetail() { }

		public Guid StockAdjustmentId { get; private set; }
		public Guid ProductVariantId { get; private set; }
		public Guid BatchId { get; private set; }
		public int AdjustmentQuantity { get; private set; }
		public int ApprovedQuantity { get; private set; }
		public string? Note { get; private set; }

		// Navigation properties
		public virtual StockAdjustment StockAdjustment { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Batch Batch { get; set; } = null!;

		// Factory methods
		public static StockAdjustmentDetail Create(
			Guid stockAdjustmentId,
			Guid productVariantId,
			Guid batchId,
			int adjustmentQuantity,
			string? note)
		{
			if (stockAdjustmentId == Guid.Empty)
				throw DomainException.BadRequest("Stock adjustment ID is required.");

			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");

			if (batchId == Guid.Empty)
				throw DomainException.BadRequest("Batch ID is required.");

			if (adjustmentQuantity == 0)
				throw DomainException.BadRequest("Adjustment quantity cannot be 0.");

			return new StockAdjustmentDetail
			{
				StockAdjustmentId = stockAdjustmentId,
				ProductVariantId = productVariantId,
				BatchId = batchId,
				AdjustmentQuantity = adjustmentQuantity,
				ApprovedQuantity = 0,
				Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
			};
		}

		// Business logic methods
		public void Approve(int approvedQuantity, string? note)
		{
			ApprovedQuantity = approvedQuantity;
			Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
		}
	}
}
