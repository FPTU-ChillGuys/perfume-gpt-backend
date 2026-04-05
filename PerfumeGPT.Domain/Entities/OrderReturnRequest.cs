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
		public Guid? ReturnShippingId { get; private set; }
		public Guid PickupAddressId { get; private set; }

		public ReturnOrderReason Reason { get; private set; }
		public string? CustomerNote { get; private set; }
		public string? StaffNote { get; private set; }
		public string? InspectionNote { get; private set; }
		public ReturnRequestStatus Status { get; private set; }

		public decimal RequestedRefundAmount { get; private set; }
		public decimal? ApprovedRefundAmount { get; private set; }
		public bool IsRefunded { get; private set; }
		public bool IsRefundOnly { get; private set; }
		public string? VnpTransactionNo { get; private set; }
		public bool IsRestocked { get; private set; }

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual User Customer { get; set; } = null!;
		public virtual User? ProcessedBy { get; set; }
		public virtual User? InspectedBy { get; set; }
		public virtual ShippingInfo? ReturnShipping { get; set; }
		public virtual ContactAddress? PickupAddress { get; set; } = null!;
		public virtual ICollection<Media> ProofImages { get; set; } = [];
		public virtual ICollection<OrderReturnRequestDetail> ReturnDetails { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Factory methods
		public static OrderReturnRequest Create(Guid orderId, Guid customerId, ReturnRequestPayload payload)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("Customer ID is required.");

			if (payload.ReturnDetails == null || payload.ReturnDetails.Count == 0)
				throw DomainException.BadRequest("At least one return detail is required.");

			if (payload.ReturnDetails.GroupBy(x => x.OrderDetailId).Any(x => x.Count() > 1))
				throw DomainException.BadRequest("Duplicate order details are not allowed in a return request.");

			var returnDetails = payload.ReturnDetails
				 .Select(x => OrderReturnRequestDetail.Create(new OrderReturnRequestDetail.ReturnRequestDetailPayload
				 {
					 OrderDetailId = x.OrderDetailId,
					 RequestedQuantity = x.RequestedQuantity,
					 OrderedQuantity = x.OrderedQuantity
				 }))
				 .ToList();

			if (payload.RequestedRefundAmount < 0)
				throw DomainException.BadRequest("Requested refund amount cannot be negative.");

			return new OrderReturnRequest
			{
				OrderId = orderId,
				CustomerId = customerId,
				Reason = payload.Reason,
				CustomerNote = string.IsNullOrWhiteSpace(payload.CustomerNote) ? null : payload.CustomerNote.Trim(),
				Status = ReturnRequestStatus.Pending,
				RequestedRefundAmount = payload.RequestedRefundAmount,
				ApprovedRefundAmount = null,
				IsRefunded = false,
				IsRefundOnly = payload.IsRefundOnly,
				ReturnDetails = returnDetails
			};
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

			if (!isApproved)
			{
				Status = ReturnRequestStatus.Rejected;
				return;
			}

			if (IsRefundOnly)
			{
				ApprovedRefundAmount = RequestedRefundAmount;
				Status = ReturnRequestStatus.ReadyForRefund;
				return;
			}

			Status = ReturnRequestStatus.ApprovedForReturn;
		}

		public void AttachReturnShipping(Guid shippingInfoId)
		{
			if (Status != ReturnRequestStatus.ApprovedForReturn)
				throw DomainException.BadRequest("Can only attach return shipping to an approved return request.");

			if (shippingInfoId == Guid.Empty)
				throw DomainException.BadRequest("Shipping Info ID is required.");

			ReturnShippingId = shippingInfoId;
		}

		public void AttachPickupAddress(Guid pickupAddressId)
		{
			if (pickupAddressId == Guid.Empty)
				throw DomainException.BadRequest("Pickup address ID is required.");

			PickupAddressId = pickupAddressId;
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

		// Records 
		public record ReturnRequestPayload
		{
			public required ReturnOrderReason Reason { get; init; }
			public required decimal RequestedRefundAmount { get; init; }
			public required bool IsRefundOnly { get; init; }
			public string? CustomerNote { get; init; }
			public required List<ReturnRequestDetailPayload> ReturnDetails { get; init; }
		}

		public record ReturnRequestDetailPayload
		{
			public required Guid OrderDetailId { get; init; }
			public required int RequestedQuantity { get; init; }
			public required int OrderedQuantity { get; init; }
		}
	}
}
