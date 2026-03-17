using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
	public class User : IdentityUser<Guid>, IHasTimestamps, ISoftDelete
	{
		public string FullName { get; set; } = string.Empty;
		public int PointBalance { get; set; } = 0;
		public bool IsActive { get; set; } = true;

		// Navigations
		public virtual CustomerProfile? CustomerProfile { get; set; }
		public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];
		public virtual ICollection<Address> Addresses { get; set; } = [];
		public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
		public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
		public virtual ICollection<CartItem> CartItems { get; set; } = [];
		public virtual Media? ProfilePicture { get; set; }
		public virtual ICollection<Order> Orders { get; set; } = [];
		public virtual ICollection<Review> Reviews { get; set; } = [];
		public virtual ICollection<Review> ModeratedReviews { get; set; } = [];
		public virtual ICollection<OrderCancelRequest> RequestedCancelRequests { get; set; } = [];
		public virtual ICollection<OrderCancelRequest> ProcessedCancelRequests { get; set; } = [];

		// Audit
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Soft Delete
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
