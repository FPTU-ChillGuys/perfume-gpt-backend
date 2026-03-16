using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class LoyaltyTransaction : BaseEntity<Guid>
	{
		public Guid UserId { get; set; }
		public Guid? VoucherId { get; set; }
		public Guid? OrderId { get; set; }
		public LoyaltyTransactionType TransactionType { get; set; }
		public int PointsChanged { get; set; }
		public string Reason { get; set; } = null!;

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual Voucher? Voucher { get; set; }
		public virtual Order? Order { get; set; }
	}
}
