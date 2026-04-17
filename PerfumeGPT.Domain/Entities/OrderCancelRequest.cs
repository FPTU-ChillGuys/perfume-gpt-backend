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

		// Đổi tên để dùng chung cho mọi phương thức
		public string? RefundTransactionReference { get; private set; }

		// THÔNG TIN NGÂN HÀNG CỦA KHÁCH CUNG CẤP (Manual Refund)
		public string? RefundBankName { get; private set; }
		public string? RefundAccountNumber { get; private set; }
		public string? RefundAccountName { get; private set; }

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
              throw DomainException.BadRequest("Order ID là bắt buộc.");

			if (requestedById == Guid.Empty)
             throw DomainException.BadRequest("Người gửi yêu cầu là bắt buộc.");

			if (payload.IsRefundRequired && (!payload.RefundAmount.HasValue || payload.RefundAmount.Value < 0))
              throw DomainException.BadRequest("Số tiền hoàn không hợp lệ.");

			if (payload.ProcessedById.HasValue && payload.ProcessedById.Value == Guid.Empty)
              throw DomainException.BadRequest("Người xử lý không hợp lệ.");

			var refundBankName = payload.RefundBankName?.Trim();
			var refundAccountNumber = payload.RefundAccountNumber?.Trim();
			var refundAccountName = payload.RefundAccountName?.Trim();

			bool hasBankInfo = !string.IsNullOrWhiteSpace(refundBankName)
				|| !string.IsNullOrWhiteSpace(refundAccountNumber)
				|| !string.IsNullOrWhiteSpace(refundAccountName);

			if (hasBankInfo)
			{
				if (string.IsNullOrWhiteSpace(refundBankName)
					|| string.IsNullOrWhiteSpace(refundAccountNumber)
					|| string.IsNullOrWhiteSpace(refundAccountName))
				{
                   throw DomainException.BadRequest("Thông tin ngân hàng chưa đầy đủ. Khi yêu cầu hoàn tiền thủ công, bắt buộc có tên ngân hàng, số tài khoản và tên chủ tài khoản.");
				}
			}

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
				IsRefunded = false,
				RefundBankName = refundBankName,
				RefundAccountNumber = refundAccountNumber,
				RefundAccountName = refundAccountName?.ToUpperInvariant()
			};
		}

		// Business logic methods
		public void Process(Guid processedById, bool isApproved, string? staffNote)
		{
			if (Status != CancelRequestStatus.Pending)
             throw DomainException.BadRequest("Yêu cầu hủy không ở trạng thái chờ xử lý.");

			if (processedById == Guid.Empty)
             throw DomainException.BadRequest("Người xử lý là bắt buộc.");

			ProcessedById = processedById;
			StaffNote = string.IsNullOrWhiteSpace(staffNote) ? null : staffNote.Trim();
			Status = isApproved ? CancelRequestStatus.Approved : CancelRequestStatus.Rejected;
		}

		public void MarkRefunded(string? transactionReference = null)
		{
			IsRefunded = true;
			RefundTransactionReference = string.IsNullOrWhiteSpace(transactionReference) ? null : transactionReference.Trim();
		}

		// Records
		public record CancelRequestPayload
		{
			public required CancelOrderReason Reason { get; init; }
			public bool IsRefundRequired { get; init; }
			public decimal? RefundAmount { get; init; }
			public Guid? ProcessedById { get; init; }
			public string? StaffNote { get; init; }

			public string? RefundBankName { get; init; }
			public string? RefundAccountNumber { get; init; }
			public string? RefundAccountName { get; init; }
		}
	}
}
