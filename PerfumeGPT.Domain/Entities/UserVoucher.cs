using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
	public class UserVoucher : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid UserId { get; set; }
		public Guid VoucherId { get; set; }
		public bool IsUsed { get; set; }

		// Navigation
		public virtual User User { get; set; } = null!;
		public virtual Voucher Voucher { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

