using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Order : BaseEntity<Guid>, IHasTimestamps
	{
		private Order() { }

		public Guid? CustomerId { get; private set; }
		public Guid? StaffId { get; private set; }
		public OrderType Type { get; private set; }
		public OrderStatus Status { get; private set; }
		public decimal TotalAmount { get; private set; }
		public PaymentStatus PaymentStatus { get; private set; }
		public Guid? UserVoucherId { get; private set; }
		public DateTime? PaymentExpiresAt { get; private set; }
		public DateTime? PaidAt { get; private set; }

		// Navigation properties
		public virtual User? Customer { get; set; }
		public virtual User? Staff { get; set; } = null!;
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = null!;
		public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = null!;
		public virtual ICollection<OrderCancelRequest> CancelRequests { get; set; } = null!;
		public virtual ICollection<OrderReturnRequest> ReturnRequests { get; set; } = null!;
		public virtual UserVoucher? UserVoucher { get; set; }
		public virtual ShippingInfo? ShippingInfo { get; set; }
		public virtual RecipientInfo RecipientInfo { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Order CreateOnline(Guid customerId, decimal totalAmount, DateTime paymentExpiresAt, List<OrderDetail> orderDetails)
		{
			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("Customer ID is required for online orders.");

			ValidateTotalAmount(totalAmount);

			return new Order
			{
				CustomerId = customerId,
				Type = OrderType.Online,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount,
				PaymentExpiresAt = paymentExpiresAt,
				OrderDetails = orderDetails
			};
		}

		public static Order CreateOffline(Guid staffId, decimal totalAmount, List<OrderDetail> orderDetails)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("Staff ID is required for offline orders.");

			ValidateTotalAmount(totalAmount);

			return new Order
			{
				StaffId = staffId,
				Type = OrderType.Offline,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount,
				OrderDetails = orderDetails
			};
		}

		// Business logic methods
		public void SetTotalAmount(decimal totalAmount)
		{
			ValidateTotalAmount(totalAmount);
			TotalAmount = totalAmount;
		}

		public void AssignVoucher(UserVoucher userVoucher)
		{
			UserVoucher = userVoucher ?? throw DomainException.BadRequest("User voucher is required.");
			UserVoucherId = userVoucher.Id;
		}

		public void SetStaff(Guid staffId)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("Staff ID is required.");

			StaffId = staffId;
		}

		public void SetStatus(OrderStatus newStatus)
		{
			if (Status == newStatus)
				throw DomainException.BadRequest("Order is already in this status.");

			var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
			{
				{ OrderStatus.Pending, [OrderStatus.Processing, OrderStatus.Cancelled] },
				{ OrderStatus.Processing, [OrderStatus.Delivering, OrderStatus.Cancelled] },
				{ OrderStatus.Delivering, [OrderStatus.Delivered, OrderStatus.Returned] },
				{ OrderStatus.Delivered, [OrderStatus.Returning, OrderStatus.Returned] },
				{ OrderStatus.Returning, [OrderStatus.Returned] },
				{ OrderStatus.Cancelled, [] },
				{ OrderStatus.Returned, [] }
			};

			if (!(validTransitions.ContainsKey(Status) && validTransitions[Status].Contains(newStatus)))
				throw DomainException.BadRequest($"Cannot change status from {Status} to {newStatus}.");

			Status = newStatus;
		}

		public void MarkPaid(DateTime paidAtUtc)
		{
			PaymentStatus = PaymentStatus.Paid;
			PaidAt = paidAtUtc;
		}

		public void MarkUnpaid()
		{
			PaymentStatus = PaymentStatus.Unpaid;
		}

		public void MarkRefunded()
		{
			PaymentStatus = PaymentStatus.Refunded;
		}

		public void SetPaymentExpiration(DateTime? paymentExpiresAt)
		{
			PaymentExpiresAt = paymentExpiresAt;
		}

		public void EnsureAddressUpdatable()
		{
			if (Status >= OrderStatus.Delivering)
				throw DomainException.BadRequest("Cannot update address after the order has started delivering.");
		}

		public void EnsureOwnedBy(Guid userId)
		{
			if (CustomerId != userId)
				throw DomainException.Forbidden("You are not authorized to modify this order.");
		}

		public void EnsureOnlineOrder()
		{
			if (Type != OrderType.Online)
				throw DomainException.BadRequest("Only online orders are supported for this operation.");
		}

		private static void ValidateTotalAmount(decimal totalAmount)
		{
			if (totalAmount < 0)
				throw DomainException.BadRequest("Total amount cannot be negative.");
		}
	}
}
