using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Voucher : BaseEntity<Guid>
	{
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }

		// Navigation
		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<Order> Orders { get; set; } = [];
	}
}
