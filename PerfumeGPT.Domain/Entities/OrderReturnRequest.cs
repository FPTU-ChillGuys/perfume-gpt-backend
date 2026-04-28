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
		public string? RefundTransactionReference { get; private set; }
		public bool IsRestocked { get; private set; }

		public string? RefundBankName { get; private set; }
		public string? RefundAccountNumber { get; private set; }
		public string? RefundAccountName { get; private set; }

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
				throw DomainException.BadRequest("ID đơn hàng là bắt buộc.");

			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("ID khách hàng là bắt buộc.");

			var returnDetails = BuildReturnDetails(payload.ReturnDetails);

			if (payload.RequestedRefundAmount < 0)
				throw DomainException.BadRequest("Số tiền hoàn trả yêu cầu không được âm.");

			bool hasBankInfo = !string.IsNullOrWhiteSpace(payload.RefundBankName) ||
						   !string.IsNullOrWhiteSpace(payload.RefundAccountNumber) ||
						   !string.IsNullOrWhiteSpace(payload.RefundAccountName);

			if (hasBankInfo)
			{
				if (string.IsNullOrWhiteSpace(payload.RefundBankName) ||
					string.IsNullOrWhiteSpace(payload.RefundAccountNumber) ||
					string.IsNullOrWhiteSpace(payload.RefundAccountName))
				{
					throw DomainException.BadRequest("Thông tin ngân hàng không đầy đủ. Tất cả các trường đều bắt buộc nếu yêu cầu hoàn tiền thủ công.");
				}
			}

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
				ReturnDetails = returnDetails,

				RefundBankName = payload.RefundBankName?.Trim(),
				RefundAccountNumber = payload.RefundAccountNumber?.Trim(),
				RefundAccountName = payload.RefundAccountName?.Trim().ToUpperInvariant()
			};
		}

		// Business logic methods
		public void Process(Guid processedById, bool isApproved, bool isRequestMoreInfo, string? staffNote)
		{
			if (Status != ReturnRequestStatus.Pending)
				throw DomainException.BadRequest("Yêu cầu hoàn trả không ở trạng thái chờ xử lý.");

			if (processedById == Guid.Empty)
				throw DomainException.BadRequest("ID người xử lý là bắt buộc.");

			if (isApproved && isRequestMoreInfo)
				throw DomainException.BadRequest("Hành động xem xét không hợp lệ.");

			ProcessedById = processedById;
			StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();

			if (isRequestMoreInfo)
			{
				if (string.IsNullOrWhiteSpace(staffNote))
					throw DomainException.BadRequest("Ghi chú của nhân viên là bắt buộc khi yêu cầu thêm thông tin.");

				Status = ReturnRequestStatus.RequestMoreInfo;
				return;
			}

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

		public void UpdateByCustomer(
			   Guid customerId,
			   string? customerNote,
			   string? refundBankName = null,
			   string? refundAccountNumber = null,
			   string? refundAccountName = null)
		{
			if (CustomerId != customerId)
				throw DomainException.Forbidden("Bạn không có quyền cập nhật yêu cầu hoàn trả này.");

			if (Status != ReturnRequestStatus.RequestMoreInfo)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả cần thêm thông tin mới có thể được cập nhật.");

			bool hasBankInfo = !string.IsNullOrWhiteSpace(refundBankName)
				|| !string.IsNullOrWhiteSpace(refundAccountNumber)
				|| !string.IsNullOrWhiteSpace(refundAccountName);

			if (hasBankInfo)
			{
				if (string.IsNullOrWhiteSpace(refundBankName)
					|| string.IsNullOrWhiteSpace(refundAccountNumber)
					|| string.IsNullOrWhiteSpace(refundAccountName))
				{
					throw DomainException.BadRequest("Thông tin ngân hàng không đầy đủ. Tất cả các trường đều bắt buộc nếu yêu cầu hoàn tiền thủ công.");
				}

				RefundBankName = refundBankName.Trim();
				RefundAccountNumber = refundAccountNumber.Trim();
				RefundAccountName = refundAccountName.Trim().ToUpperInvariant();
			}

			CustomerNote = string.IsNullOrWhiteSpace(customerNote) ? null : customerNote.Trim();
			ApprovedRefundAmount = null;
			StaffNote = null;
			InspectionNote = null;
			IsRestocked = false;
			RefundTransactionReference = null;

			Status = ReturnRequestStatus.Pending;
		}

		public void OverrideRefundAmount(decimal newAmount, string? note)
		{
			if (Status != ReturnRequestStatus.ReadyForRefund)
				throw DomainException.BadRequest("Chỉ có thể ghi đè số tiền khi yêu cầu hoàn trả đang ở trạng thái sẵn sàng hoàn tiền.");

			if (newAmount <= 0)
				throw DomainException.BadRequest("Số tiền hoàn trả được phê duyệt phải lớn hơn 0.");

			var oldAmount = ApprovedRefundAmount ?? 0m;

			// Nếu không có sự thay đổi về số tiền thì không cần làm gì cả
			if (newAmount == oldAmount)
				return;

			// Tạo log kiểm toán (Audit Log)
			string cleanNote = string.IsNullOrWhiteSpace(note) ? "Không có ghi chú" : note.Trim();
			string auditLog = $"\n[Tài chính - Ghi đè] Đã thay đổi số tiền hoàn từ {oldAmount:N0} thành {newAmount:N0}. Ghi chú: {cleanNote}";

			// Nối log mới vào StaffNote hiện tại
			StaffNote = string.IsNullOrWhiteSpace(StaffNote)
				? auditLog.Trim()
				: StaffNote + auditLog;

			// Cập nhật số tiền mới
			ApprovedRefundAmount = newAmount;
		}

		public void CancelByCustomer(Guid customerId)
		{
			if (CustomerId != customerId)
				throw DomainException.Forbidden("Bạn không có quyền hủy yêu cầu hoàn trả này.");

			if (Status != ReturnRequestStatus.Pending && Status != ReturnRequestStatus.RequestMoreInfo)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả đang chờ xử lý hoặc cần thêm thông tin mới có thể bị hủy.");

			Status = ReturnRequestStatus.Rejected;
		}

		private static List<OrderReturnRequestDetail> BuildReturnDetails(List<ReturnRequestDetailPayload> returnRequestDetails)
		{
			if (returnRequestDetails == null || returnRequestDetails.Count == 0)
				throw DomainException.BadRequest("Ít nhất một chi tiết hoàn trả là bắt buộc.");

			if (returnRequestDetails.GroupBy(x => x.OrderDetailId).Any(x => x.Count() > 1))
				throw DomainException.BadRequest("Không được phép có chi tiết đơn hàng trùng lặp trong yêu cầu hoàn trả.");

			return [.. returnRequestDetails
				.Select(x => OrderReturnRequestDetail.Create(new OrderReturnRequestDetail.ReturnRequestDetailPayload
				{
					OrderDetailId = x.OrderDetailId,
					RequestedQuantity = x.RequestedQuantity,
					OrderedQuantity = x.OrderedQuantity
				}))];
		}

		public void AttachReturnShipping(Guid shippingInfoId)
		{
			if (Status != ReturnRequestStatus.ApprovedForReturn)
				throw DomainException.BadRequest("Chỉ có thể gắn thông tin vận chuyển trả hàng cho yêu cầu hoàn trả đã được phê duyệt.");

			if (shippingInfoId == Guid.Empty)
				throw DomainException.BadRequest("ID thông tin vận chuyển là bắt buộc.");

			ReturnShippingId = shippingInfoId;
		}

		public void AttachPickupAddress(Guid pickupAddressId)
		{
			if (pickupAddressId == Guid.Empty)
				throw DomainException.BadRequest("ID địa chỉ lấy hàng là bắt buộc.");

			PickupAddressId = pickupAddressId;
		}

		public void StartInspection(Guid inspectedById, string? inspectionNote = null)
		{
			if (Status != ReturnRequestStatus.ApprovedForReturn)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả đã được phê duyệt mới có thể chuyển sang kiểm tra.");

			if (inspectedById == Guid.Empty)
				throw DomainException.BadRequest("Người kiểm tra là bắt buộc.");

			InspectedById = inspectedById;
			InspectionNote = string.IsNullOrWhiteSpace(inspectionNote) ? null : inspectionNote.Trim();
			Status = ReturnRequestStatus.Inspecting;
		}

		public void RecordInspectionResult(decimal approvedRefundAmount, bool isRestocked, string? inspectionNote = null)
		{
			if (Status != ReturnRequestStatus.Inspecting)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả đang kiểm tra mới có thể ghi nhận kết quả kiểm tra.");

			if (approvedRefundAmount < 0)
				throw DomainException.BadRequest("Số tiền hoàn trả được phê duyệt không được âm.");

			ApprovedRefundAmount = approvedRefundAmount;
			IsRestocked = isRestocked;
			InspectionNote = string.IsNullOrWhiteSpace(inspectionNote) ? InspectionNote : inspectionNote.Trim();

			Status = ReturnRequestStatus.ReadyForRefund;
		}

		public void RejectAfterInspection(Guid inspectedById, string note)
		{
			if (Status != ReturnRequestStatus.Inspecting)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả đang kiểm tra mới có thể bị từ chối sau khi kiểm tra.");
			if (inspectedById == Guid.Empty)
				throw DomainException.BadRequest("Người kiểm tra là bắt buộc.");
			if (string.IsNullOrWhiteSpace(note))
				throw DomainException.BadRequest("Ghi chú từ chối là bắt buộc.");
			InspectedById = inspectedById;
			InspectionNote = note.Trim();
			Status = ReturnRequestStatus.Rejected;
		}

		public void CancelBySystemWhenReturnPickupFailed(string? note = null)
		{
			if (Status != ReturnRequestStatus.ApprovedForReturn)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả đã được phê duyệt mới có thể bị hủy do thất bại trong việc lấy hàng trả lại.");

			if (!string.IsNullOrWhiteSpace(note))
			{
				StaffNote = note?.Trim();
			}

			Status = ReturnRequestStatus.Rejected;
		}

		public void MarkRefunded(string? transactionReference = null)
		{
			if (Status != ReturnRequestStatus.ReadyForRefund)
				throw DomainException.BadRequest("Chỉ các yêu cầu hoàn trả sẵn sàng cho việc hoàn tiền mới có thể được hoàn tất.");

			if (!ApprovedRefundAmount.HasValue)
				throw DomainException.BadRequest("Số tiền hoàn trả được phê duyệt là bắt buộc trước khi hoàn tiền.");

			if (IsRefunded)
				throw DomainException.BadRequest("Yêu cầu hoàn trả đã được hoàn tiền.");

			IsRefunded = true;
			RefundTransactionReference = string.IsNullOrWhiteSpace(transactionReference) ? null : transactionReference.Trim();
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

			public string? RefundBankName { get; init; }
			public string? RefundAccountNumber { get; init; }
			public string? RefundAccountName { get; init; }
		}

		public record ReturnRequestDetailPayload
		{
			public required Guid OrderDetailId { get; init; }
			public required int RequestedQuantity { get; init; }
			public required int OrderedQuantity { get; init; }
		}
	}
}
