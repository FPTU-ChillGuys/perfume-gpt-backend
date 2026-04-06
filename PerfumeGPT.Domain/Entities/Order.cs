using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Commons.Helpers;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Order : BaseEntity<Guid>, IHasTimestamps
	{
		private Order() { }

		public Guid? CustomerId { get; private set; }
		public Guid? StaffId { get; private set; }
		public string Code { get; private set; } = null!;
		public OrderType Type { get; private set; }
		public OrderStatus Status { get; private set; }
		public decimal TotalAmount { get; private set; }
		public PaymentStatus PaymentStatus { get; private set; }
		public Guid? UserVoucherId { get; private set; }
		public DateTime? PaymentExpiresAt { get; private set; }
		public DateTime? PaidAt { get; private set; }
		public Guid? ForwardShippingId { get; private set; }
		public Guid? ContactAddressId { get; private set; }

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
		public virtual ShippingInfo? ForwardShipping { get; set; }
		public virtual ContactAddress? ContactAddress { get; set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Order CreateOnline(Guid customerId, decimal totalAmount, DateTime paymentExpiresAt)
		{
			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("Customer ID is required for online orders.");

			ValidateTotalAmount(totalAmount);

			return new Order
			{
				CustomerId = customerId,
				Code = OrderCodeGenerator.Generate(OrderType.Online),
				Type = OrderType.Online,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount,
				PaymentExpiresAt = paymentExpiresAt
			};
		}

		public static Order CreateOffline(Guid? customerId, Guid staffId, decimal totalAmount)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("Staff ID is required for offline orders.");

			ValidateTotalAmount(totalAmount);

			return new Order
			{
				CustomerId = customerId,
				StaffId = staffId,
				Code = OrderCodeGenerator.Generate(OrderType.Offline),
				Type = OrderType.Offline,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount
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

		public void AddOrderDetail(Guid variantId, int quantity, decimal unitPrice, string snapshot)
		{
			var orderDetail = OrderDetail.Create(variantId, quantity, unitPrice, snapshot);
			AddOrderDetail(orderDetail);
		}

		public void AddOrderDetails(IEnumerable<OrderDetail> orderDetails)
		{
			if (orderDetails is null)
				throw DomainException.BadRequest("Order details are required.");

			foreach (var orderDetail in orderDetails)
			{
				AddOrderDetail(orderDetail);
			}
		}

		private void AddOrderDetail(OrderDetail orderDetail)
		{
			if (orderDetail is null)
				throw DomainException.BadRequest("Order detail is required.");

			orderDetail.Order = this;
			OrderDetails.Add(orderDetail);
		}

		public void SetStaff(Guid staffId)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("Staff ID is required.");

			StaffId = staffId;
		}

		public void SetStatus(OrderStatus newStatus)
		{
			if (Status == newStatus && newStatus != OrderStatus.Pending)
				throw DomainException.BadRequest("Order is already in this status.");

			var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
			{
				{ OrderStatus.Pending, [OrderStatus.Pending, OrderStatus.Preparing, OrderStatus.Delivered, OrderStatus.Cancelled] },

				{ OrderStatus.Preparing, [OrderStatus.ReadyToPick, OrderStatus.Cancelled] },

				{ OrderStatus.ReadyToPick, [OrderStatus.Delivering, OrderStatus.Delivered, OrderStatus.Cancelled] },

				{ OrderStatus.Delivering, [OrderStatus.Delivered, OrderStatus.Returning, OrderStatus.Cancelled] },

				{ OrderStatus.Delivered, [OrderStatus.Returning, OrderStatus.Returned] },

				{ OrderStatus.Returning, [OrderStatus.Returned, OrderStatus.Partial_Returned] },
				{ OrderStatus.Cancelled, [] },
				{ OrderStatus.Partial_Returned, [] },
				{ OrderStatus.Returned, [] }
			};

			if (!(validTransitions.ContainsKey(Status) && validTransitions[Status].Contains(newStatus)))
				throw DomainException.BadRequest($"Cannot change status from {Status} to {newStatus}.");

			if (Type == OrderType.Offline && (newStatus == OrderStatus.Preparing || newStatus == OrderStatus.ReadyToPick || newStatus == OrderStatus.Delivering))
			{
				throw DomainException.BadRequest("Offline POS orders skip packaging and delivering phases and must go directly to Delivered.");
			}

			Status = newStatus;
		}

		public void AttachForwardShipping(Guid shippingInfoId)
		{
			if (shippingInfoId == Guid.Empty)
				throw DomainException.BadRequest("Shipping Info ID is required.");

			ForwardShippingId = shippingInfoId;
		}

		public void AttachContactAddress(Guid contactAddressId)
		{
			if (contactAddressId == Guid.Empty)
				throw DomainException.BadRequest("Contact address ID is required.");

			EnsureAddressUpdatable();

			ContactAddressId = contactAddressId;
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

		public void MarkPartiallyRefunded()
		{
			PaymentStatus = PaymentStatus.Partial_Refunded;
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
