using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.OrderCancelRequest;

namespace PerfumeGPT.Application.Services
{
	public class OrderWorkflowService : IOrderWorkflowService
	{
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<OrderWorkflowService> _logger;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IOrderCancellationFinalizeService _orderCancellationFinalizeService;

		public OrderWorkflowService(
			IBackgroundJobService backgroundJobService,
			ILogger<OrderWorkflowService> logger,
			IUnitOfWork unitOfWork,
			IOrderCancellationFinalizeService orderCancellationFinalizeService)
		{
			_backgroundJobService = backgroundJobService;
			_logger = logger;
			_unitOfWork = unitOfWork;
			_orderCancellationFinalizeService = orderCancellationFinalizeService;
		}

		public async Task ProcessForwardShippingStatusAsync(Order order, ShippingStatus newShippingStatus, DateTime? deliveredAtUtc = null)
		{
			switch (newShippingStatus)
			{
				case ShippingStatus.Delivering:
					if (order.Status == OrderStatus.ReadyToPick)
						order.SetStatus(OrderStatus.Delivering);
					break;

				case ShippingStatus.Delivered:
					if (order.Status == OrderStatus.ReadyToPick)
						order.SetStatus(OrderStatus.Delivering); // Tua nhanh nếu rớt webhook

					if (order.Status != OrderStatus.Delivered)
						order.SetStatus(OrderStatus.Delivered);

					if (order.PaymentStatus != PaymentStatus.Paid)
						order.RecordPayment(order.RemainingAmount > 0 ? order.RemainingAmount : order.TotalAmount, DateTime.UtcNow);

					var pendingCod = order.PaymentTransactions?.FirstOrDefault(t => t.Method == PaymentMethod.CashOnDelivery && t.TransactionStatus == TransactionStatus.Pending);
					if (pendingCod != null)
					{
						pendingCod.MarkSuccess("Thu hộ COD thành công bởi GHN");
						_unitOfWork.Payments.Update(pendingCod);
					}

					if (order.CustomerId.HasValue)
					{
						int points = (int)(order.TotalAmount / 1000m);
						if (points > 0)
						{
							var deliveredAt = deliveredAtUtc ?? DateTime.UtcNow;
							var storePolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
							_backgroundJobService.ScheduleLoyaltyPointsGrant(_logger, order.Id, deliveredAt, storePolicy?.OrderRewardPointsInDays ?? 0);
						}
					}
					break;

				case ShippingStatus.Returning:
					if (order.Status == OrderStatus.ReadyToPick || order.Status == OrderStatus.Delivering)
						order.SetStatus(OrderStatus.Returning);
					break;

				case ShippingStatus.Returned:
					// GHN returned ở chiều giao đi: giao thất bại, hàng quay về kho => coi là hủy đơn (không phải return-after-delivery).
					if (order.Status != OrderStatus.Cancelled)
					{
						await _orderCancellationFinalizeService.FinalizeOrderCancellationAsync(order, CancelOrderReason.UnreachableCustomer, true);
					}
					order.MarkRefusedByCustomer();

					if (order.PaidAmount > 0)
					{
						var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
						if (!hasPendingRequest)
						{
							// Trừ đi khoản phạt bom hàng theo mức cọc chính sách (snapshot khi tạo đơn).
							var actualRefund = Math.Max(0, order.PaidAmount - order.PolicyDepositAmount);
							if (actualRefund <= 0)
							{
								EnqueueNoRefundRoleNotifications(order.Code, newShippingStatus, order.PolicyDepositAmount);
								EnqueueNoRefundNotification(order, order.PolicyDepositAmount);
								break;
							}

							var payload = new CancelRequestPayload
							{
								Reason = CancelOrderReason.UnreachableCustomer, // Lý do Bom hàng
								IsRefundRequired = actualRefund > 0,
								RefundAmount = actualRefund > 0 ? actualRefund : null,
								StaffNote = $"Hệ thống tạo yêu cầu hoàn tiền do khách bom hàng. Đã khấu trừ phạt cọc theo chính sách: {order.PolicyDepositAmount:N0}đ."
							};

							var requesterId = ResolveAutoRequesterId(order);
							if (requesterId.HasValue)
							{
								var cancelReq = OrderCancelRequest.Create(order.Id, requesterId.Value, payload);
								await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
								EnqueueAutoRefundApprovalNotifications(cancelReq.Id, order.Code, newShippingStatus);
							}
						}
					}

					EnqueueCustomerCancellationNotification(order, newShippingStatus);
					break;

				case ShippingStatus.Damaged:
				case ShippingStatus.Lost:
					// GHN damage / lost: kết thúc đơn như hủy (hoàn KM, kho, voucher) nhưng ShippingInfo giữ Damaged/Lost (đã set ở TryApplyShippingStatus).
					if (order.Status != OrderStatus.Cancelled)
					{
						await _orderCancellationFinalizeService.FinalizeOrderCancellationAsync(order, CancelOrderReason.Other, false);
					}

					if (order.PaidAmount > 0)
					{
						var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
						if (!hasPendingRequest)
						{
							var staffNote = newShippingStatus == ShippingStatus.Damaged
								? "Hệ thống tự động tạo yêu cầu hoàn tiền do GHN báo hàng hư hỏng (damage)."
								: "Hệ thống tự động tạo yêu cầu hoàn tiền do GHN báo thất lạc (lost).";

							var payload = new CancelRequestPayload
							{
								Reason = CancelOrderReason.Other,
								IsRefundRequired = true,
								RefundAmount = order.PaidAmount, // BẮT BUỘC ĐỔI THÀNH PaidAmount (Hoàn đúng số tiền khách đã trả/cọc)
								StaffNote = staffNote
							};

							var requesterId = ResolveAutoRequesterId(order);
							if (requesterId.HasValue)
							{
								var cancelReq = OrderCancelRequest.Create(order.Id, requesterId.Value, payload);
								await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
								EnqueueAutoRefundApprovalNotifications(cancelReq.Id, order.Code, newShippingStatus);
							}
						}
					}

					EnqueueCustomerCancellationNotification(order, newShippingStatus);
					break;

				case ShippingStatus.Cancelled:
					// GHN "cancel": hủy vận đơn trước khi giao (tương đương hủy đơn thường), khác damage/lost.
					// State machine GHN: https://api.ghn.vn/home/docs/detail?id=85
					if (order.Status != OrderStatus.Cancelled)
					{
						await _orderCancellationFinalizeService.FinalizeOrderCancellationAsync(order, CancelOrderReason.Other, true);
					}

					if (order.PaidAmount > 0)
					{
						var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
						if (!hasPendingRequest)
						{
							var payload = new CancelRequestPayload
							{
								Reason = CancelOrderReason.Other,
								IsRefundRequired = true,
								RefundAmount = order.PaidAmount, // BẮT BUỘC ĐỔI THÀNH PaidAmount (Hoàn đúng số tiền khách đã trả/cọc)
								StaffNote = "Hệ thống tự động tạo yêu cầu hoàn tiền do GHN hủy vận đơn (cancel — đơn không được giao)."
							};

							var requesterId = ResolveAutoRequesterId(order);
							if (requesterId.HasValue)
							{
								var cancelReq = OrderCancelRequest.Create(order.Id, requesterId.Value, payload);
								await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
								EnqueueAutoRefundApprovalNotifications(cancelReq.Id, order.Code, newShippingStatus);
							}
						}
					}

					EnqueueCustomerCancellationNotification(order, newShippingStatus);
					break;
			}
		}

		private Guid? ResolveAutoRequesterId(Order order)
		{
			if (order.CustomerId.HasValue)
				return order.CustomerId.Value;

			if (order.StaffId.HasValue)
				return order.StaffId.Value;

			_logger.LogWarning("Skip creating auto cancel request for order {OrderId} because requester cannot be resolved.", order.Id);
			return null;
		}

		private void EnqueueCustomerCancellationNotification(Order order, ShippingStatus shippingStatus)
		{
			if (!order.CustomerId.HasValue)
				return;

			var reasonText = shippingStatus switch
			{
				ShippingStatus.Cancelled => "đơn vận chuyển bị hủy bởi GHN",
				ShippingStatus.Returned => "GHN hoàn hàng do không liên lạc được với bạn hoặc không thể giao thành công",
				ShippingStatus.Damaged => "GHN báo hàng bị hư hỏng trong quá trình vận chuyển",
				ShippingStatus.Lost => "GHN báo hàng bị thất lạc trong quá trình vận chuyển",
				_ => "đơn hàng không thể tiếp tục giao"
			};

			_backgroundJobService.EnqueueCustomerNotificationWithFcm(
				_logger,
				order.CustomerId.Value,
				"Đơn hàng đã bị hủy",
				$"Đơn hàng #{order.Code} đã bị hủy do {reasonText}.",
				NotificationType.Warning,
				order.Id,
				NotifiReferecneType.Order);
		}

		private void EnqueueNoRefundNotification(Order order, decimal penaltyAmount)
		{
			if (!order.CustomerId.HasValue)
				return;

			_backgroundJobService.EnqueueCustomerNotificationWithFcm(
				_logger,
				order.CustomerId.Value,
				"Đơn hàng không phát sinh hoàn tiền",
				$"Đơn hàng #{order.Code} đã bị hủy và không có khoản hoàn tiền do đã khấu trừ tiền cọc ({penaltyAmount:N0}đ).",
				NotificationType.Warning,
				order.Id,
				NotifiReferecneType.Order);
		}

		private void EnqueueNoRefundRoleNotifications(string orderCode, ShippingStatus shippingStatus, decimal penaltyAmount)
		{
			var shippingFailureText = shippingStatus switch
			{
				ShippingStatus.Returned => "GHN hoàn hàng do giao không thành công",
				ShippingStatus.Cancelled => "GHN hủy vận đơn",
				ShippingStatus.Damaged => "GHN báo hàng hư hỏng",
				ShippingStatus.Lost => "GHN báo hàng thất lạc",
				_ => "sự cố giao hàng"
			};

			var title = "Đơn hủy không phát sinh hoàn tiền";
			var message = $"Đơn #{orderCode} bị hủy do {shippingFailureText}. Không tạo yêu cầu hoàn tiền vì số hoàn sau khi trừ phạt cọc là 0đ (mức phạt: {penaltyAmount:N0}đ).";

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.staff,
				title,
				message,
				NotificationType.Warning,
				null,
				NotifiReferecneType.Order);

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.admin,
				title,
				message,
				NotificationType.Warning,
				null,
				NotifiReferecneType.Order);
		}

		private void EnqueueAutoRefundApprovalNotifications(Guid cancelRequestId, string orderCode, ShippingStatus shippingStatus)
		{
			var shippingFailureText = shippingStatus switch
			{
				ShippingStatus.Cancelled => "GHN hủy vận đơn",
				ShippingStatus.Damaged => "GHN báo hàng hư hỏng",
				ShippingStatus.Lost => "GHN báo hàng thất lạc",
				ShippingStatus.Returned => "GHN hoàn hàng do giao không thành công",
				_ => "sự cố giao hàng"
			};

			var title = "Yêu cầu duyệt hoàn tiền tự động";
			var message = $"Hệ thống tự động tạo yêu cầu duyệt hoàn tiền cho đơn #{orderCode} do {shippingFailureText}.";

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.staff,
				title,
				message,
				NotificationType.Warning,
				cancelRequestId,
				NotifiReferecneType.OrderCancelRequest);

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.admin,
				title,
				message,
				NotificationType.Warning,
				cancelRequestId,
				NotifiReferecneType.OrderCancelRequest);
		}
	}
}
