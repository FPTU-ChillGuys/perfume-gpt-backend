using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class Order : BaseEntity<Guid>
	{
		public Guid? CustomerId { get; set; }
		public Guid StaffId { get; set; }
		public int TypeId { get; set; }
		public string? Status { get; set; }
		public decimal TotalAmount { get; set; }
		public string? PaymentStatus { get; set; }
		public string? ExternalShopeeId { get; set; }
		public Guid? VoucherId { get; set; }

		// Navigation
		public virtual User? Customer { get; set; }
		public virtual User Staff { get; set; } = null!;
		public virtual OrderType OrderType { get; set; } = null!;
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;
		public virtual ShippingInfo? ShippingInfo { get; set; }
		public virtual RecipientInfo RecipientInfo { get; set; } = null!;
		public virtual Voucher? Voucher { get; set; }
	}
}
