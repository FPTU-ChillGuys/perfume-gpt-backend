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
		public decimal RequiredDepositAmount { get; private set; }
		public decimal PaidAmount { get; private set; } = 0;
		public decimal RemainingAmount => TotalAmount - PaidAmount;
		public PaymentStatus PaymentStatus { get; private set; }
		public Guid? UserVoucherId { get; private set; }
		public DateTime? PaymentExpiresAt { get; private set; }
		public DateTime? PaidAt { get; private set; }
		public Guid? ForwardShippingId { get; private set; }
		public Guid? ContactAddressId { get; private set; }
		public string? GuestEmailOrPhone { get; private set; }

		// Navigation properties
		public virtual User? Customer { get; set; }
		public virtual User? Staff { get; set; } = null!;
		public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
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
		public static Order CreateOnline(Guid customerId, decimal totalAmount, StorePolicy currentSetting, bool isCodOrder)
		{
			if (currentSetting is null)
				throw DomainException.BadRequest("Cấu hình đặt cọc là bắt buộc.");

			if (customerId == Guid.Empty)
				throw DomainException.BadRequest("ID khách hàng là bắt buộc cho đơn hàng online.");

			ValidateTotalAmount(totalAmount);

			decimal depositAmount = isCodOrder && currentSetting.IsDepositRequiredForCOD
				  ? decimal.Round(totalAmount * currentSetting.RequiredDepositPercentage / 100m, 0, MidpointRounding.AwayFromZero)
				  : 0;

			return new Order
			{
				CustomerId = customerId,
				Code = OrderCodeGenerator.Generate(OrderType.Online),
				Type = OrderType.Online,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount,
				RequiredDepositAmount = depositAmount,
				PaidAmount = 0,
				PaymentExpiresAt = depositAmount > 0 ? DateTime.UtcNow.AddMinutes(currentSetting.DepositTimeoutMinutes) : null
			};
		}

		// Đã thêm storePolicy và isCodOrder vào tham số
		public static Order CreateOffline(Guid? customerId, string? guestEmailOrPhone, Guid staffId, decimal totalAmount, StorePolicy? currentSetting, bool isCodOrder)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("ID nhân viên là bắt buộc cho đơn hàng offline.");

			ValidateTotalAmount(totalAmount);

			// Tính tiền cọc nếu là đơn COD
			decimal depositAmount = isCodOrder && currentSetting != null && currentSetting.IsDepositRequiredForCOD
				  ? decimal.Round(totalAmount * currentSetting.RequiredDepositPercentage / 100m, 0, MidpointRounding.AwayFromZero)
				  : 0;

			return new Order
			{
				CustomerId = customerId,
				GuestEmailOrPhone = guestEmailOrPhone,
				StaffId = staffId,
				Code = OrderCodeGenerator.Generate(OrderType.Offline),
				Type = OrderType.Offline,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				TotalAmount = totalAmount,
				RequiredDepositAmount = depositAmount,
				PaidAmount = 0,
				// Gán thời gian hết hạn thanh toán nếu có cọc
				PaymentExpiresAt = depositAmount > 0 ? DateTime.UtcNow.AddMinutes(currentSetting!.DepositTimeoutMinutes) : null
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
			UserVoucher = userVoucher ?? throw DomainException.BadRequest("ID voucher người dùng là bắt buộc.");
			UserVoucherId = userVoucher.Id;
		}

		public void AddOrderDetails(IEnumerable<OrderDetail> orderDetails)
		{
			if (orderDetails is null)
				throw DomainException.BadRequest("Chi tiết đơn hàng là bắt buộc.");

			foreach (var orderDetail in orderDetails)
			{
				AddOrderDetail(orderDetail);
			}
		}

		private void AddOrderDetail(OrderDetail orderDetail)
		{
			if (orderDetail is null)
				throw DomainException.BadRequest("Chi tiết đơn hàng là bắt buộc.");

			orderDetail.Order = this;
			OrderDetails.Add(orderDetail);
		}

		public void SetStaff(Guid staffId)
		{
			if (staffId == Guid.Empty)
				throw DomainException.BadRequest("ID nhân viên là bắt buộc.");

			StaffId = staffId;
		}

		public void SetStatus(OrderStatus newStatus)
		{
			if (Status == newStatus && newStatus != OrderStatus.Pending)
				throw DomainException.BadRequest("Đơn hàng đã ở trạng thái này.");

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
				throw DomainException.BadRequest($"Không thể thay đổi trạng thái từ {Status} sang {newStatus}.");

			if (Type == OrderType.Offline && (newStatus == OrderStatus.Preparing || newStatus == OrderStatus.ReadyToPick || newStatus == OrderStatus.Delivering))
			{
				throw DomainException.BadRequest("Đơn hàng POS offline bỏ qua các giai đoạn đóng gói và giao hàng và phải đi thẳng đến Đã giao.");
			}

			Status = newStatus;
		}

		public void MarkReturnedByPartner()
		{
			SetStatus(OrderStatus.Returned);
			if (CustomerId.HasValue)
			{
				AddDomainEvent(new Events.OrderRefusedDomainEvent(CustomerId.Value, Id));
			}
		}

		public void CancelCashInStore(CancelOrderReason cancelReason)
		{
			if (Status != OrderStatus.ReadyToPick)
				throw DomainException.BadRequest("Chỉ các đơn hàng ReadyToPick CashInStore mới có thể bị hủy với hình phạt.");

			SetStatus(OrderStatus.Cancelled);

			// Chỉ phạt nếu lý do là khách không đến
			if (CustomerId.HasValue && cancelReason == CancelOrderReason.SuspectedFraud)
			{
				AddDomainEvent(new Events.OrderRefusedDomainEvent(CustomerId.Value, Id));
			}
		}

		public void AttachForwardShipping(Guid shippingInfoId)
		{
			if (shippingInfoId == Guid.Empty)
				throw DomainException.BadRequest("ID thông tin vận chuyển là bắt buộc.");

			ForwardShippingId = shippingInfoId;
		}

		public void AttachContactAddress(Guid contactAddressId)
		{
			if (contactAddressId == Guid.Empty)
				throw DomainException.BadRequest("ID địa chỉ liên hệ là bắt buộc.");

			EnsureAddressUpdatable();

			ContactAddressId = contactAddressId;
		}

		//public void MarkPaid(DateTime paidAtUtc)
		//{
		//	RecordPayment(Math.Max(0, RemainingAmount), paidAtUtc);
		//}

		//public void MarkUnpaid()
		//{
		//	PaymentStatus = PaidAmount >= RequiredDepositAmount && PaidAmount > 0
		//		  ? PaymentStatus.PartialPaid
		//		  : PaymentStatus.Unpaid;
		//}

		public void RecordPayment(decimal amountReceived, DateTime paidAtUtc)
		{
			if (amountReceived <= 0)
				throw DomainException.BadRequest("Số tiền phải lớn hơn 0.");

			PaidAmount += amountReceived;

			if (PaidAmount >= TotalAmount)
			{
				PaidAmount = TotalAmount;
				PaymentStatus = PaymentStatus.Paid;
				PaidAt = paidAtUtc;
			}
			else if (PaidAmount >= RequiredDepositAmount)
			{
				PaymentStatus = PaymentStatus.PartialPaid;
			}
		}

		public void MarkRefunded()
		{
			PaymentStatus = PaymentStatus.Refunded;
		}

		public void MarkPartiallyRefunded()
		{
			PaymentStatus = PaymentStatus.PartialRefunded;
		}

		public void SetPaymentExpiration(DateTime? paymentExpiresAt)
		{
			PaymentExpiresAt = paymentExpiresAt;
		}

		public void EnsureAddressUpdatable()
		{
			if (Status >= OrderStatus.Delivering)
				throw DomainException.BadRequest("Không thể cập nhật địa chỉ sau khi đơn hàng đã bắt đầu giao.");
		}

		public void EnsureOwnedBy(Guid userId)
		{
			if (CustomerId != userId)
				throw DomainException.Forbidden("Bạn không được phép sửa đổi đơn hàng này.");
		}

		public void EnsureOnlineOrder()
		{
			if (Type != OrderType.Online)
				throw DomainException.BadRequest("Chỉ các đơn hàng trực tuyến mới được hỗ trợ cho thao tác này.");
		}

		public void FulfillOrderDetail(Guid orderDetailId, Guid fulfilledBatchId)
		{
			if (orderDetailId == Guid.Empty)
				throw DomainException.BadRequest("ID chi tiết đơn hàng là bắt buộc.");

			var orderDetail = OrderDetails.FirstOrDefault(od => od.Id == orderDetailId)
				?? throw DomainException.NotFound("Chi tiết đơn hàng không tìm thấy trong đơn hàng này.");

			orderDetail.Fulfill(fulfilledBatchId);
		}

		private static void ValidateTotalAmount(decimal totalAmount)
		{
			if (totalAmount < 0)
				throw DomainException.BadRequest("Tổng số tiền không được âm.");
		}
	}
}
