using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderWorkflowService : IOrderWorkflowService
	{
		private readonly ILoyaltyTransactionService _loyaltyService;
		private readonly IAuditScope _auditScope;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockReservationService _stockReservationService;
		private readonly IVoucherService _voucherService;

		public OrderWorkflowService(ILoyaltyTransactionService loyaltyService, IAuditScope auditScope, IUnitOfWork unitOfWork, IStockReservationService stockReservationService, IVoucherService voucherService)
		{
			_loyaltyService = loyaltyService;
			_auditScope = auditScope;
			_unitOfWork = unitOfWork;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
		}

		public async Task ProcessShippingStatusChangeAsync(Order order, ShippingStatus newShippingStatus)
		{
			switch (newShippingStatus)
			{
				case ShippingStatus.Delivering:
					if (order.Status == OrderStatus.ReadyToPick)
						order.SetStatus(OrderStatus.Delivering);
					break;

				case ShippingStatus.Delivered:
					if (order.Status == OrderStatus.ReadyToPick)
						order.SetStatus(OrderStatus.Delivering); // Tua nhanh nếu rớt webhook Delivering

					if (order.Status != OrderStatus.Delivered)
						order.SetStatus(OrderStatus.Delivered);

					// Chỉ MarkPaid nếu đơn chưa thanh toán (COD)
					if (order.PaymentStatus != PaymentStatus.Paid)
					{
						order.MarkPaid(DateTime.UtcNow);
					}

					// Chốt giao dịch COD thành Success
                   var pendingCod = order.PaymentTransactions?.FirstOrDefault(t => t.Method == PaymentMethod.CashOnDelivery && t.TransactionStatus == TransactionStatus.Pending);
					if (pendingCod != null)
					{
						pendingCod.MarkSuccess("Thu hộ COD thành công bởi GHN");
						_unitOfWork.Payments.Update(pendingCod);
					}

					if (order.CustomerId.HasValue)
					{
						using (_auditScope.BeginSystemAction())
						{
							int points = (int)(order.TotalAmount * 0.01m);
							if (points > 0)
								await _loyaltyService.PlusPointAsync(order.CustomerId.Value, points, order.Id, false);
						}
					}
					break;

				case ShippingStatus.Returning:
					if (order.Status == OrderStatus.ReadyToPick || order.Status == OrderStatus.Delivering)
					{
						order.SetStatus(OrderStatus.Returning);
					}
					break;

				case ShippingStatus.Returned:
					// Tua nhanh qua trạng thái Returning nếu rớt Webhook
					if (order.Status == OrderStatus.ReadyToPick || order.Status == OrderStatus.Delivering)
						order.SetStatus(OrderStatus.Returning);

					if (order.Status != OrderStatus.Returned)
						order.SetStatus(OrderStatus.Returned);

					// Huỷ khoản nợ COD (Khách không nhận, không thu được tiền)
                    var failedCod = order.PaymentTransactions?.FirstOrDefault(t => t.Method == PaymentMethod.CashOnDelivery && t.TransactionStatus == TransactionStatus.Pending);
					if (failedCod != null)
					{
						failedCod.MarkCancelled("Khách không nhận hàng, hoàn về kho.");
						_unitOfWork.Payments.Update(failedCod);
					}

					// Nhập lại hàng vào kho (Restock) vì nó đã bị trừ lúc Fulfill
					await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(order.Id);

					//if (order.PaymentStatus == PaymentStatus.Paid)
					//{
					//	var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
					//	if (!hasPendingRequest)
					//	{
					//		var payload = new CancelRequestPayload
					//		{
					//			Reason = "Giao hàng thất bại (Khách không nhận/GHN hoàn hàng).",
					//			IsRefundRequired = true,
					//			RefundAmount = order.TotalAmount,
					//			StaffNote = "Hệ thống tự động tạo yêu cầu hoàn tiền do sự cố giao vận."
					//		};

					//		// Tạo Request dưới danh nghĩa Customer (hoặc tạo một System Admin ID riêng)
					//		var cancelReq = OrderCancelRequest.Create(order.Id, order.CustomerId ?? Guid.Empty, payload);
					//		await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
					//	}
					//}
					break;

				case ShippingStatus.Cancelled:
					// Kịch bản: GHN làm mất hàng, hoặc xe tới lấy mà không có hàng nên GHN huỷ vận đơn
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

						//if (order.PaymentStatus == PaymentStatus.Paid)
						//{
						//	var hasPendingRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(r => r.OrderId == order.Id && r.Status == CancelRequestStatus.Pending);
						//	if (!hasPendingRequest)
						//	{
						//		var payload = new CancelRequestPayload
						//		{
						//			Reason = "Đơn vị giao vận huỷ đơn (Thất lạc/Hư hỏng).",
						//			IsRefundRequired = true,
						//			RefundAmount = order.TotalAmount,
						//			StaffNote = "Hệ thống tự động tạo yêu cầu hoàn tiền do GHN huỷ vận đơn."
						//		};

						//		var cancelReq = OrderCancelRequest.Create(order.Id, order.CustomerId ?? Guid.Empty, payload);
						//		await _unitOfWork.OrderCancelRequests.AddAsync(cancelReq);
						//	}
						//}
					}
					break;
			}
		}
	}
}
