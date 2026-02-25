using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class UserVoucher : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid UserId { get; set; }
		public Guid VoucherId { get; set; }
		//public Guid OrderId { get; set; }
		public bool IsUsed { get; set; }
		public UsageStatus Status { get; set; }

		// Navigation
		public virtual User User { get; set; } = null!;
		public virtual Voucher Voucher { get; set; } = null!;
		//public virtual Order Order { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

