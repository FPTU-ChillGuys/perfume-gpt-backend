using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Order : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid? CustomerId { get; set; }
		public Guid? StaffId { get; set; }
		public OrderType Type { get; set; }
		public OrderStatus Status { get; set; }
		public decimal TotalAmount { get; set; }
		public PaymentStatus PaymentStatus { get; set; }
		public string? ExternalShopeeId { get; set; }
		public Guid? VoucherId { get; set; }
		public DateTime? PaymentExpiresAt { get; set; }
		public DateTime? PaidAt { get; set; }
		public bool IsExpired => !PaidAt.HasValue && PaymentExpiresAt.HasValue && DateTime.UtcNow > PaymentExpiresAt.Value;

		// Navigation
		public virtual User? Customer { get; set; }
		public virtual User? Staff { get; set; } = null!;
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = null!;
		public virtual ShippingInfo? ShippingInfo { get; set; }
		public virtual RecipientInfo RecipientInfo { get; set; } = null!;
		public virtual Voucher? Voucher { get; set; }

		// Audit
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
