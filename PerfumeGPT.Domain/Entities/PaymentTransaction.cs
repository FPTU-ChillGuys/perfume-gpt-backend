using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class PaymentTransaction : BaseEntity<Guid>, IHasTimestamps
	{
		protected PaymentTransaction() { }

		public Guid OrderId { get; private set; }
		public PaymentMethod Method { get; private set; }
		public TransactionStatus TransactionStatus { get; private set; }
		public TransactionType TransactionType { get; private set; }
		public string? GatewayTransactionNo { get; private set; }
		public string? FailureReason { get; private set; }
		public decimal Amount { get; private set; }

		// Retry & Refund tracking
		public Guid? OriginalPaymentId { get; private set; }
		public int RetryAttempt { get; private set; } = 0;

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual Receipt Receipt { get; set; } = null!;
		public virtual PaymentTransaction? OriginalPayment { get; set; }
		public virtual ICollection<PaymentTransaction> RetryPayments { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static PaymentTransaction Create(Guid orderId, PaymentMethod method, decimal amount)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (amount <= 0)
				throw DomainException.BadRequest("Payment amount must be greater than 0.");

			return new PaymentTransaction
			{
				OrderId = orderId,
				Method = method,
				Amount = amount,
				TransactionType = TransactionType.Payment,
				TransactionStatus = TransactionStatus.Pending,
				RetryAttempt = 0
			};
		}

		public static PaymentTransaction CreateRefund(Guid orderId, Guid originalPaymentId, PaymentMethod method, decimal refundAmount)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			if (originalPaymentId == Guid.Empty)
				throw DomainException.BadRequest("Original Payment ID is required for a refund.");

			if (refundAmount <= 0)
				throw DomainException.BadRequest("Refund amount must be greater than 0.");

			return new PaymentTransaction
			{
				OrderId = orderId,
				OriginalPaymentId = originalPaymentId,
				Method = method,
				Amount = -refundAmount,
				TransactionType = TransactionType.Refund,
				TransactionStatus = TransactionStatus.Pending,
				RetryAttempt = 0
			};
		}

		// Business logic methods
		public bool IsPending() => TransactionStatus == TransactionStatus.Pending;

		public void EnsurePending()
		{
			if (!IsPending())
				throw DomainException.BadRequest("Payment is not pending.");
		}

		public void MarkSuccess(string? gatewayTransactionNo = null)
		{
			EnsurePending();
			TransactionStatus = TransactionStatus.Success;
			FailureReason = null;
			if (gatewayTransactionNo != null)
			{
				GatewayTransactionNo = gatewayTransactionNo;
			}
		}

		public void MarkFailed(string? reason = null, string? gatewayTransactionNo = null)
		{
			EnsurePending();
			TransactionStatus = TransactionStatus.Failed;
			FailureReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

			if (!string.IsNullOrWhiteSpace(gatewayTransactionNo))
			{
				GatewayTransactionNo = gatewayTransactionNo.Trim();
			}
		}

		public void MarkCancelled(string reason)
		{
			if (!IsPending())
				throw DomainException.BadRequest("Only pending payments can be cancelled.");

			TransactionStatus = TransactionStatus.Cancelled;
			FailureReason = reason;
		}

		public PaymentTransaction CreateRetry(PaymentMethod method)
		{
			if (TransactionStatus == TransactionStatus.Success)
				throw DomainException.BadRequest("Cannot retry completed payments.");

			return new PaymentTransaction
			{
				OrderId = OrderId,
				Method = method,
				Amount = Amount,
				TransactionType = TransactionType,
				TransactionStatus = TransactionStatus.Pending,
				OriginalPaymentId = OriginalPaymentId ?? Id,
				RetryAttempt = RetryAttempt + 1
			};
		}
	}
}
