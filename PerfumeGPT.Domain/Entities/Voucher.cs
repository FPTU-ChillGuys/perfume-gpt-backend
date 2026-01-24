using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Voucher : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }

		// Implement not yet
		//public int TotalQuantity { get; set; }
		//public int RemainingQuantity { get; set; }
		//public int MaxUsagePerUser { get; set; } = 1;
		//public bool IsPublic { get; set; }

		// Navigation
		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<Order> Orders { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}

