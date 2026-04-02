using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderCancelRequest : BaseEntity<Guid>, IHasTimestamps
	{
		protected OrderCancelRequest() { }

		public Guid OrderId { get; private set; }
		public Guid RequestedById { get; private set; }
		public Guid? ProcessedById { get; private set; }
		public CancelOrderReason Reason { get; private set; }
		public string? StaffNote { get; private set; }
		public CancelRequestStatus Status { get; private set; }

		public bool IsRefundRequired { get; private set; }
		public decimal? RefundAmount { get; private set; }
		public bool IsRefunded { get; private set; }
		public string? VnpTransactionNo { get; private set; }

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual User RequestedBy { get; set; } = null!;
		public virtual User? ProcessedBy { get; set; }

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory methods
		public static OrderCancelRequest Create(Guid orderId, Guid requestedById, CancelRequestPayload payload)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (requestedById == Guid.Empty)
				throw DomainException.BadRequest("Requested by user is required.");

			if (payload.IsRefundRequired && (!payload.RefundAmount.HasValue || payload.RefundAmount.Value < 0))
				throw DomainException.BadRequest("Refund amount is invalid.");

			if (payload.ProcessedById.HasValue && payload.ProcessedById.Value == Guid.Empty)
				throw DomainException.BadRequest("Processed by user is invalid.");

			return new OrderCancelRequest
			{
				OrderId = orderId,
				RequestedById = requestedById,
				ProcessedById = payload.ProcessedById,
				Reason = payload.Reason,
				StaffNote = string.IsNullOrWhiteSpace(payload.StaffNote) ? null : payload.StaffNote.Trim(),
				Status = CancelRequestStatus.Pending,
				IsRefundRequired = payload.IsRefundRequired,
				RefundAmount = payload.RefundAmount,
				IsRefunded = false
			};
		}

		// Business logic methods
		public void Process(Guid processedById, bool isApproved, string? staffNote)
		{
			if (Status != CancelRequestStatus.Pending)
				throw DomainException.BadRequest("Cancel request is not pending.");

			if (processedById == Guid.Empty)
				throw DomainException.BadRequest("Processed by user is required.");

			ProcessedById = processedById;
			StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
			Status = isApproved ? CancelRequestStatus.Approved : CancelRequestStatus.Rejected;
		}

		public void MarkRefunded(string? vnpTransactionNo = null)
		{
			IsRefunded = true;
			VnpTransactionNo = string.IsNullOrWhiteSpace(vnpTransactionNo) ? null : vnpTransactionNo.Trim();
		}

		// Records
		public record CancelRequestPayload
		{
			public required CancelOrderReason Reason { get; init; }
			public bool IsRefundRequired { get; init; }
			public decimal? RefundAmount { get; init; }
			public Guid? ProcessedById { get; init; }
			public string? StaffNote { get; init; }
		}
	}
}
