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
		private readonly IStockReservationService _stockReservationService;
		private readonly IVoucherService _voucherService;

		public OrderWorkflowService(
			IBackgroundJobService backgroundJobService,
			ILogger<OrderWorkflowService> logger,
			IUnitOfWork unitOfWork,
			IStockReservationService stockReservationService,
			IVoucherService voucherService)
		{
			_backgroundJobService = backgroundJobService;
			_logger = logger;
			_unitOfWork = unitOfWork;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
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
							_backgroundJobService.ScheduleLoyaltyPointsGrant(_logger, order.Id, deliveredAt);
						}
					}
					break;

				case ShippingStatus.Returning:
					if (order.Status == OrderStatus.ReadyToPick || order.Status == OrderStatus.Delivering)
						order.SetStatus(OrderStatus.Returning);
					break;

				case ShippingStatus.Returned:
					if (order.Status == OrderStatus.ReadyToPick || order.Status == OrderStatus.Delivering)
						order.SetStatus(OrderStatus.Returning);

					if (order.Status != OrderStatus.Returned)
						order.MarkReturnedByPartner(); // Hàm này đã có Domain Event phạt user boom hàng

					var failedCod = order.PaymentTransactions?.FirstOrDefault(t => t.Method == PaymentMethod.CashOnDelivery && t.TransactionStatus == TransactionStatus.Pending);
					if (failedCod != null)
					{
						failedCod.MarkCancelled("Khách không nhận hàng, hoàn về kho.");
						_unitOfWork.Payments.Update(failedCod);
					}

					await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(order.Id);

					if (order.PaidAmount > 0)
					{
						var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
						if (!hasPendingRequest)
						{
							// Trừ đi khoản phạt bom hàng (100% tiền cọc)
							var actualRefund = Math.Max(0, order.PaidAmount - order.RequiredDepositAmount);

							var payload = new CancelRequestPayload
							{
								Reason = CancelOrderReason.UnreachableCustomer, // Lý do Bom hàng
								IsRefundRequired = actualRefund > 0,
								RefundAmount = actualRefund > 0 ? actualRefund : null,
								StaffNote = $"Hệ thống tạo yêu cầu hoàn tiền do khách bom hàng. Đã khấu trừ phạt cọc: {order.RequiredDepositAmount:N0}đ."
							};
							var cancelReq = OrderCancelRequest.Create(order.Id, order.CustomerId ?? Guid.Empty, payload);
							await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
						}
					}
					break;

				case ShippingStatus.Cancelled:
					// Kịch bản: GHN làm mất hàng, hoặc lấy hàng thất bại ở Chiều Đi
					// KHÔNG CÒN logic check ReturnRequest ở đây nữa!
					if (order.Status != OrderStatus.Cancelled)
					{
						order.SetStatus(OrderStatus.Cancelled);

						var cancelCod = order.PaymentTransactions?.FirstOrDefault(t => t.Method == PaymentMethod.CashOnDelivery && t.TransactionStatus == TransactionStatus.Pending);
						if (cancelCod != null)
						{
							cancelCod.MarkCancelled("Giao vận bị huỷ.");
							_unitOfWork.Payments.Update(cancelCod);
						}

						await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(order.Id);
						await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);

						if (order.PaymentStatus == PaymentStatus.Paid)
						{
							var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
							if (!hasPendingRequest)
							{
								var payload = new CancelRequestPayload
								{
									Reason = CancelOrderReason.Other,
									IsRefundRequired = true,
									RefundAmount = order.TotalAmount,
									StaffNote = "Hệ thống tự động tạo yêu cầu hoàn tiền do GHN huỷ vận đơn."
								};
								var cancelReq = OrderCancelRequest.Create(order.Id, order.CustomerId ?? Guid.Empty, payload);
								await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
							}
						}
					}
					break;
			}
		}
	}
}
