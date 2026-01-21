using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class PaymentTransaction : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid OrderId { get; set; }
		public PaymentMethod Method { get; set; }
		public TransactionStatus TransactionStatus { get; set; }
		public decimal Amount { get; set; }

		// Retry tracking
		public Guid? OriginalPaymentId { get; set; }
		public int RetryAttempt { get; set; } = 0;

		// Navigation
		public virtual Order Order { get; set; } = null!;
		public virtual Receipt Receipt { get; set; } = null!;
		public virtual PaymentTransaction? OriginalPayment { get; set; }
		public virtual ICollection<PaymentTransaction> RetryPayments { get; set; } = new List<PaymentTransaction>();

		// Timestamps
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
