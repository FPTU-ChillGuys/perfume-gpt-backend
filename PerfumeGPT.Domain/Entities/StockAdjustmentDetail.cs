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
		public static StockAdjustmentDetail Create(StockAdjustmentDetailPayload payload)
		{
			if (payload.ProductVariantId == Guid.Empty)
                throw DomainException.BadRequest("Product variant ID là bắt buộc.");

			if (payload.BatchId == Guid.Empty)
              throw DomainException.BadRequest("Batch ID là bắt buộc.");

			if (payload.AdjustmentQuantity == 0)
               throw DomainException.BadRequest("Số lượng điều chỉnh không được bằng 0.");

			return new StockAdjustmentDetail
			{
				ProductVariantId = payload.ProductVariantId,
				BatchId = payload.BatchId,
				AdjustmentQuantity = payload.AdjustmentQuantity,
				ApprovedQuantity = 0,
				Note = string.IsNullOrWhiteSpace(payload.Note) ? null : payload.Note.Trim()
			};
		}

		// Business logic methods
		public void Approve(int approvedQuantity, string? note)
		{
			ApprovedQuantity = approvedQuantity;
			Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
		}

		// Records
		public record StockAdjustmentDetailPayload
		{
			public required Guid ProductVariantId { get; init; }
			public required Guid BatchId { get; init; }
			public required int AdjustmentQuantity { get; init; }
			public string? Note { get; init; }
		}
	}
}
