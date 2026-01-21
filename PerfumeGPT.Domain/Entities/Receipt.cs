using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
	public class Receipt : BaseEntity<Guid>, IHasCreatedAt
	{
		public Guid TransactionId { get; set; }
		public string ReceiptNumber { get; set; } = null!;

		// Navigation
		public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }
	}
}
