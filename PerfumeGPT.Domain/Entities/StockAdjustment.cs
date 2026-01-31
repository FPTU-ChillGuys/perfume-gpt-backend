using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class StockAdjustment : BaseEntity<Guid>, IUpdateAuditable, IHasCreatedAt, ISoftDelete
	{
		public Guid CreatedById { get; set; }
		public Guid? VerifiedById { get; set; }
		public DateTime AdjustmentDate { get; set; }
		public StockAdjustmentReason Reason { get; set; }
		public string? Note { get; set; }
		public StockAdjustmentStatus Status { get; set; }

		// Navigation
		public virtual User CreatedByUser { get; set; } = null!;
		public virtual User? VerifiedByUser { get; set; }
		public virtual ICollection<StockAdjustmentDetail> AdjustmentDetails { get; set; } = [];

		// Audit
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		// Soft Delete
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
