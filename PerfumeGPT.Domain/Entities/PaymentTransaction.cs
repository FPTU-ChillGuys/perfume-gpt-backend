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
		public string? FailureReason { get; private set; }
		public decimal Amount { get; private set; }

		// Retry tracking
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

		public void MarkSuccess()
		{
			EnsurePending();
			TransactionStatus = TransactionStatus.Success;
			FailureReason = null;
		}

		public void MarkFailed(string? reason = null)
		{
			EnsurePending();
			TransactionStatus = TransactionStatus.Failed;
			FailureReason = reason;
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
				TransactionStatus = TransactionStatus.Pending,
				OriginalPaymentId = OriginalPaymentId ?? Id,
				RetryAttempt = RetryAttempt + 1
			};
		}
	}
}
