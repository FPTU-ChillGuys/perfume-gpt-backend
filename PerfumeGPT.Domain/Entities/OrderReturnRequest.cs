using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderReturnRequest : BaseEntity<Guid>, IHasTimestamps
	{
		protected OrderReturnRequest() { }

		public Guid OrderId { get; private set; }
		public Guid CustomerId { get; private set; }
		public Guid? ProcessedById { get; private set; }
		public Guid? InspectedById { get; private set; }
		public ReturnOrderReason Reason { get; private set; }
		public string? CustomerNote { get; private set; }
		public string? StaffNote { get; private set; }
		public string? InspectionNote { get; private set; }
		public ReturnRequestStatus Status { get; private set; }

		public decimal RequestedRefundAmount { get; private set; }
		public decimal? ApprovedRefundAmount { get; private set; }
		public bool IsRefunded { get; private set; }
		public string? VnpTransactionNo { get; private set; }
		public bool IsRestocked { get; private set; }

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual User Customer { get; set; } = null!;
		public virtual User? ProcessedBy { get; set; }
		public virtual User? InspectedBy { get; set; }
		public virtual ICollection<Media> ProofImages { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory methods
		public static OrderReturnRequest Create(
			Guid orderId,
			Guid customerId,
			ReturnOrderReason reason,
			decimal requestedRefundAmount,
		  string? customerNote = null)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("Customer ID is required.");

			if (requestedRefundAmount < 0)
				throw DomainException.BadRequest("Requested refund amount cannot be negative.");

			var request = new OrderReturnRequest
			{
				OrderId = orderId,
				CustomerId = customerId,
				Reason = reason,
				CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim(),
				Status = ReturnRequestStatus.Pending,
				RequestedRefundAmount = requestedRefundAmount,
				ApprovedRefundAmount = null,
				IsRefunded = false
			};

			return request;
		}

		// Business logic methods
		public void Process(Guid processedById, bool isApproved, string? staffNote)
		{
			if (Status != ReturnRequestStatus.Pending)
				throw DomainException.BadRequest("Return request is not pending.");

			if (processedById == Guid.Empty)
				throw DomainException.BadRequest("Processed by user is required.");

			ProcessedById = processedById;
			StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
			Status = isApproved ? ReturnRequestStatus.ApprovedForReturn : ReturnRequestStatus.Rejected;
		}

		public void StartInspection(Guid inspectedById, string? inspectionNote = null)
		{
			if (Status != ReturnRequestStatus.ApprovedForReturn)
				throw DomainException.BadRequest("Only approved return requests can be moved to inspection.");

			if (inspectedById == Guid.Empty)
				throw DomainException.BadRequest("Inspected by user is required.");

			InspectedById = inspectedById;
			InspectionNote = string.IsNullOrWhiteSpace(inspectionNote) ? null : inspectionNote.Trim();
			Status = ReturnRequestStatus.Inspecting;
		}

		public void RecordInspectionResult(decimal approvedRefundAmount, bool isRestocked, string? inspectionNote = null)
		{
			if (Status != ReturnRequestStatus.Inspecting)
				throw DomainException.BadRequest("Only inspecting return requests can record inspection results.");

			if (approvedRefundAmount < 0)
				throw DomainException.BadRequest("Approved refund amount cannot be negative.");

			ApprovedRefundAmount = approvedRefundAmount;
			IsRestocked = isRestocked;
			InspectionNote = string.IsNullOrWhiteSpace(inspectionNote) ? InspectionNote : inspectionNote.Trim();

			Status = ReturnRequestStatus.ReadyForRefund;
		}

		public void RejectAfterInspection(Guid inspectedById, string note)
		{
			if (Status != ReturnRequestStatus.Inspecting)
				throw DomainException.BadRequest("Only inspecting return requests can be rejected after inspection.");
			if (inspectedById == Guid.Empty)
				throw DomainException.BadRequest("Inspected by user is required.");
			if (string.IsNullOrWhiteSpace(note))
				throw DomainException.BadRequest("Rejection note is required.");
			InspectedById = inspectedById;
			InspectionNote = note.Trim();
			Status = ReturnRequestStatus.Rejected;
		}

		public void MarkRefunded(string? vnpTransactionNo = null)
		{
			if (Status != ReturnRequestStatus.ReadyForRefund)
				throw DomainException.BadRequest("Only return requests ready for refund can be completed.");

			if (!ApprovedRefundAmount.HasValue)
				throw DomainException.BadRequest("Approved refund amount is required before refunding.");

			if (IsRefunded)
				throw DomainException.BadRequest("Return request is already refunded.");

			IsRefunded = true;
			VnpTransactionNo = string.IsNullOrWhiteSpace(vnpTransactionNo) ? null : vnpTransactionNo.Trim();
			Status = ReturnRequestStatus.Completed;
		}
	}
}
