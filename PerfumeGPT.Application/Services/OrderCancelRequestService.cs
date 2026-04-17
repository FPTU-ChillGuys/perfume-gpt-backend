using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderCancelRequestService : IOrderCancelRequestService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVnPayService _vnPayService;
		private readonly IMomoService _momoService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IStockReservationService _stockReservationService;
		private readonly IVoucherService _voucherService;
		private readonly IGHNService _ghnService;
		private readonly INotificationService _notificationService;

		public OrderCancelRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			IStockReservationService stockReservationService,
			IVoucherService voucherService,
			IGHNService ghnService,
			INotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
			_ghnService = ghnService;
			_notificationService = notificationService;
		}
		#endregion Dependencies



		public async Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedRequestsAsync(GetPagedCancelRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderCancelRequests.GetPagedResponsesAsync(request);
			var canViewFullBankInfo = _httpContextAccessor.HttpContext?.User.IsInRole(UserRole.admin.ToString()) == true;

			if (!canViewFullBankInfo)
			{
				items = [.. items.Select(i => i with
				{
					RefundAccountNumber = MaskAccountNumber(i.RefundAccountNumber)
				})];
			}

			return BaseResponse<PagedResult<OrderCancelRequestResponse>>.Ok(
				new PagedResult<OrderCancelRequestResponse>(items, request.PageNumber, request.PageSize, totalCount)
			);
		}

		public async Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedUserRequestsAsync(Guid userId, GetPagedCancelRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderCancelRequests.GetPagedUserResponsesAsync(userId, request);
			items = [.. items.Select(i => i with
			{
				RefundAccountNumber = MaskAccountNumber(i.RefundAccountNumber)
			})];

			return BaseResponse<PagedResult<OrderCancelRequestResponse>>.Ok(
				new PagedResult<OrderCancelRequestResponse>(items, request.PageNumber, request.PageSize, totalCount)
			);
		}

		public async Task<BaseResponse<OrderCancelRequestResponse>> GetRequestByIdAsync(Guid requestId, Guid requesterId, bool isPrivilegedUser)
		{
			var request = await _unitOfWork.OrderCancelRequests.GetResponseByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu hủy đơn.");

			if (!isPrivilegedUser && request.RequestedById != requesterId)
				throw AppException.Forbidden("Bạn không có quyền xem yêu cầu hủy đơn này.");

			if (!isPrivilegedUser)
			{
				request = request with
				{
					RefundAccountNumber = MaskAccountNumber(request.RefundAccountNumber)
				};
			}

			return BaseResponse<OrderCancelRequestResponse>.Ok(request, "Lấy thông tin yêu cầu hủy đơn thành công.");
		}

		private static string? MaskAccountNumber(string? accountNumber)
		{
			if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
				return accountNumber;

			return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
		}

		public async Task<BaseResponse<string>> ProcessRequestAsync(Guid requestId, Guid processedBy, string userRole, ProcessCancelRequest request)
		{
			var cancelRequest = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu hủy đơn.");

			if (cancelRequest.Status != CancelRequestStatus.Pending)
				throw AppException.BadRequest("Yêu cầu này đã được xử lý trước đó.");

			if (cancelRequest.IsRefundRequired && userRole != UserRole.admin.ToString())
			{
				throw AppException.Forbidden("Chỉ Quản trị viên mới có thể duyệt yêu cầu hủy đơn có hoàn tiền.");
			}

			string? refundTransactionNo = null;
			string? refundMessage = null;
			PaymentTransaction? originalPayment = null;
			decimal? finalRefundAmount = null;

			var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
			  ?? throw AppException.NotFound("Không tìm thấy đơn hàng liên quan.");

			if (request.IsApproved)
			{
				if (cancelRequest.IsRefundRequired)
				{
					var refundMethod = request.RefundMethod
						   ?? throw AppException.BadRequest("Bắt buộc chọn phương thức hoàn tiền khi cần hoàn tiền.");

					var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
						p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success))
						.OrderByDescending(p => p.CreatedAt).ToList();

					originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == refundMethod) ?? throw AppException.NotFound($"Không tìm thấy giao dịch {refundMethod} thành công cho đơn hàng này.");

					var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext hiện không khả dụng.");
					var refundAmount = ResolveRefundAmount(cancelRequest, order, originalPayment.Amount);
					var isRefundSuccess = false;

					switch (refundMethod)
					{
						case PaymentMethod.VnPay:
							originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == refundMethod)
						  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {refundMethod} thành công cho đơn hàng này.");

							var refundAmountVnPay = ResolveRefundAmount(cancelRequest, order, originalPayment.Amount);
							var vnPayResponse = await _vnPayService.RefundAsync(context, new VnPayRefundRequest
							{
								OrderId = order.Id,
								Amount = refundAmountVnPay,
								PaymentId = originalPayment.Id,
								TransactionType = refundAmountVnPay == originalPayment.Amount ? "02" : "03",
								TransactionNo = originalPayment.GatewayTransactionNo,
								CreateBy = processedBy.ToString(),
								OrderInfo = $"Hoàn tiền cho đơn hàng {order.Code}",
								TransactionDate = originalPayment.CreatedAt.ToString("yyyyMMddHHmmss")
							});

							isRefundSuccess = vnPayResponse.IsSuccess;
							refundMessage = vnPayResponse.Message;
							refundTransactionNo = vnPayResponse.TransactionNo;
							break;

						case PaymentMethod.Momo:
							originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == refundMethod)
						  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {refundMethod} thành công cho đơn hàng này.");

							var refundAmountMomo = ResolveRefundAmount(cancelRequest, order, originalPayment.Amount);
							var momoResponse = await _momoService.RefundAsync(context, new MomoRefundRequest
							{
								OrderId = order.Id,
								OrderCode = order.Code,
								Amount = refundAmountMomo,
								PaymentId = originalPayment.Id,
								TransactionNo = originalPayment.GatewayTransactionNo,
								Description = $"Hoàn tiền cho đơn hàng {order.Code}"
							});

							isRefundSuccess = momoResponse.IsSuccess;
							refundMessage = momoResponse.Message;
							refundTransactionNo = momoResponse.TransactionNo;
							break;

						case PaymentMethod.ExternalBankTransfer:
						case PaymentMethod.CashInStore:
							originalPayment = successfulOnlinePayments.FirstOrDefault()
							   ?? throw AppException.NotFound("Không tìm thấy giao dịch thanh toán thành công của đơn hàng để đối chiếu hoàn tiền thủ công.");

							if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
								throw AppException.BadRequest("Bắt buộc nhập mã tham chiếu giao dịch thủ công cho hoàn tiền chuyển khoản.");

							isRefundSuccess = true;
							refundMessage = request.StaffNote ?? "Đã ghi nhận hoàn tiền thủ công bởi Quản trị viên.";
							refundTransactionNo = request.ManualTransactionReference.Trim();
							break;

						default:
							throw AppException.BadRequest($"Không hỗ trợ hoàn tiền cho phương thức thanh toán {refundMethod}.");
					}

					finalRefundAmount = ResolveRefundAmount(cancelRequest, order, originalPayment.Amount);

					if (!isRefundSuccess)
					{
						var failedRefund = PaymentTransaction.CreateRefund(
							orderId: order.Id,
							originalPaymentId: originalPayment.Id,
						   method: refundMethod,
						 refundAmount: finalRefundAmount.Value
						);

						failedRefund.MarkFailed(
							reason: refundMessage,
							gatewayTransactionNo: refundTransactionNo
						);

						await _unitOfWork.Payments.AddAsync(failedRefund);
						await _unitOfWork.SaveChangesAsync();

						throw AppException.BadRequest($"Hoàn tiền qua {refundMethod} thất bại. Đã hủy xử lý yêu cầu hủy đơn. Lý do: {refundMessage}");
					}
				}

				if (!string.IsNullOrWhiteSpace(order.ForwardShipping?.TrackingNumber))
				{
					try
					{
						await _ghnService.CancelOrderAsync(new CancelOrderRequest
						{
							TrackingNumbers = [order.ForwardShipping.TrackingNumber]
						});
					}
					catch (Exception ex)
					{
						throw AppException.BadRequest($"Hủy đơn vận chuyển trên GHN thất bại: {ex.Message}");
					}
				}
			}

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var freshCancelReq = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
					 ?? throw AppException.NotFound("Không tìm thấy yêu cầu hủy đơn trong phiên giao dịch.");

				  var freshOrder = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
				   ?? throw AppException.NotFound("Không tìm thấy đơn hàng liên quan trong phiên giao dịch.");

				  freshCancelReq.Process(processedBy, request.IsApproved, request.StaffNote);

				  if (request.IsApproved)
				  {
					  if (freshCancelReq.IsRefundRequired && originalPayment != null)
					  {
						  freshCancelReq.MarkRefunded(refundTransactionNo);
						  freshOrder.MarkRefunded();

						  var refundPayment = PaymentTransaction.CreateRefund(
							  orderId: freshOrder.Id,
							  originalPaymentId: originalPayment.Id,
							  method: originalPayment.Method,
							  refundAmount: finalRefundAmount ?? ResolveRefundAmount(freshCancelReq, freshOrder, originalPayment.Amount)
						  );

						  refundPayment.MarkSuccess(refundTransactionNo);

						  await _unitOfWork.Payments.AddAsync(refundPayment);
					  }

					  freshOrder.SetStatus(OrderStatus.Cancelled);
					  _unitOfWork.Orders.Update(freshOrder);

					  if (freshOrder.ForwardShipping != null)
					  {
						  freshOrder.ForwardShipping.Cancel();
						  _unitOfWork.ShippingInfos.Update(freshOrder.ForwardShipping);
					  }

					  await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(freshOrder.Id);

					  if (freshOrder.UserVoucherId.HasValue)
						  await _voucherService.RefundVoucherForCancelledOrderAsync(freshOrder.Id);
				  }

				  _unitOfWork.OrderCancelRequests.Update(freshCancelReq);

				  return BaseResponse<string>.Ok(request.IsApproved ? "Yêu cầu hủy đơn đã được chấp thuận và xử lý." : "Yêu cầu hủy đơn đã bị từ chối.");
			  });

			await _notificationService.SendToUserAsync(
				cancelRequest.RequestedById,
				"Kết quả yêu cầu hủy đơn",
				$"Yêu cầu hủy đơn #{cancelRequest.OrderId} của bạn đã được {(request.IsApproved ? "chấp thuận" : "từ chối")}.",
				request.IsApproved ? NotificationType.Success : NotificationType.Warning,
				referenceId: cancelRequest.Id,
				referenceType: NotifiReferecneType.OrderCancelRequest);

			return response;
		}

		private static decimal ResolveRefundAmount(OrderCancelRequest cancelRequest, Order order, decimal originalPaymentAmount)
		{
			if (IsAutoLogisticsFailureCancellation(cancelRequest, order))
			{
				var shippingFee = order.ForwardShipping?.ShippingFee ?? 0m;
				var penaltyShipping = shippingFee * 1.5m;
				return Math.Max(0m, order.TotalAmount - penaltyShipping);
			}

			return cancelRequest.RefundAmount ?? originalPaymentAmount;
		}

		private static bool IsAutoLogisticsFailureCancellation(OrderCancelRequest cancelRequest, Order order)
		{
			if (!cancelRequest.IsRefundRequired)
				return false;

			if (order.ForwardShipping == null)
				return false;

			var isShippingFailureStatus = order.Status is OrderStatus.Returned or OrderStatus.Cancelled;
			var isShippingFailureCarrierStatus = order.ForwardShipping.Status is ShippingStatus.Returned or ShippingStatus.Cancelled;

			return isShippingFailureStatus && isShippingFailureCarrierStatus;
		}
	}
}