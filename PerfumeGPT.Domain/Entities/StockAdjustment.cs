using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class StockAdjustment : BaseEntity<Guid>, IUpdateAuditable, IHasCreatedAt, ISoftDelete
	{
		protected StockAdjustment() { }

		public Guid CreatedById { get; private set; }
		public Guid? VerifiedById { get; private set; }
		public DateTime AdjustmentDate { get; private set; }
		public StockAdjustmentReason Reason { get; private set; }
		public string? Note { get; private set; }
		public StockAdjustmentStatus Status { get; private set; }

		// Navigation properties
		public virtual User CreatedByUser { get; set; } = null!;
		public virtual User? VerifiedByUser { get; set; }
		public virtual ICollection<StockAdjustmentDetail> AdjustmentDetails { get; set; } = [];

		// IUpdateAuditable and IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// Factory methods
		public static StockAdjustment Create(Guid createdById, DateTime adjustmentDate, StockAdjustmentReason reason, string? note)
		{
			if (createdById == Guid.Empty)
				throw DomainException.BadRequest("Created by user is required.");

			return new StockAdjustment
			{
				CreatedById = createdById,
				AdjustmentDate = adjustmentDate,
				Reason = reason,
				Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
				Status = StockAdjustmentStatus.Pending
			};
		}

		// Business logic methods
		public StockAdjustmentDetail AddDetail(Guid productVariantId, Guid batchId, int adjustmentQuantity, string? note)
		{
			// Cha gọi Factory method của Con
			var detail = StockAdjustmentDetail.Create(productVariantId, batchId, adjustmentQuantity, note);
			AdjustmentDetails.Add(detail);

			return detail;
		}

		public void AddApprovedDetail(Guid productVariantId, Guid batchId, int adjustmentQuantity, int approvedQuantity, string? note)
		{
			var detail = StockAdjustmentDetail.Create(productVariantId, batchId, adjustmentQuantity, note);
			detail.Approve(approvedQuantity, note);

			AdjustmentDetails.Add(detail);
		}

		public void EnsureVerifiable()
		{
			if (Status != StockAdjustmentStatus.InProgress)
				throw DomainException.BadRequest("Only in progress stock adjustments can be verified.");
		}

		public void Complete(Guid verifiedByUserId)
		{
			if (verifiedByUserId == Guid.Empty)
				throw DomainException.BadRequest("Verified by user is required.");

			EnsureVerifiable();
			VerifiedById = verifiedByUserId;
			Status = StockAdjustmentStatus.Completed;
		}

		public void Cancel(string? cancelReason = null)
		{
			if (Status == StockAdjustmentStatus.Completed || Status == StockAdjustmentStatus.Cancelled)
				throw DomainException.BadRequest($"Cannot cancel a stock adjustment that is already {Status}.");

			Status = StockAdjustmentStatus.Cancelled;

			if (!string.IsNullOrWhiteSpace(cancelReason))
			{
				var formattedReason = cancelReason.Trim();
				Note = string.IsNullOrWhiteSpace(Note) ? $"Canceled: {formattedReason}" : $"{Note} | Canceled: {formattedReason}";
			}
		}

		public void UpdateStatus(StockAdjustmentStatus newStatus)
		{
			if (Status == newStatus)
				throw DomainException.BadRequest("Stock adjustment is already in this status.");

			var validTransitions = new Dictionary<StockAdjustmentStatus, List<StockAdjustmentStatus>>
			{
				{ StockAdjustmentStatus.Pending, [StockAdjustmentStatus.InProgress, StockAdjustmentStatus.Completed, StockAdjustmentStatus.Cancelled] },
				{ StockAdjustmentStatus.InProgress, [StockAdjustmentStatus.Completed, StockAdjustmentStatus.Cancelled] },
				{ StockAdjustmentStatus.Completed, [] },
				{ StockAdjustmentStatus.Cancelled, [] }
			};

			if (!(validTransitions.ContainsKey(Status) && validTransitions[Status].Contains(newStatus)))
				throw DomainException.BadRequest($"Cannot change status from {Status} to {newStatus}.");

			Status = newStatus;
		}

		public void EnsureDeletable()
		{
			if (Status == StockAdjustmentStatus.Completed)
				throw DomainException.BadRequest("Completed stock adjustments cannot be deleted.");

			if (Status == StockAdjustmentStatus.InProgress)
				throw DomainException.BadRequest("In-progress stock adjustments cannot be deleted.");
		}
	}
}
