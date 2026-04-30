using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
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
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<OrderCancelRequestService> _logger;

		public OrderCancelRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			IStockReservationService stockReservationService,
			IVoucherService voucherService,
			IBackgroundJobService backgroundJobService,
			ILogger<OrderCancelRequestService> logger)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
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

		public async Task<BaseResponse<string>> ProcessRefundAsync(Guid requestId, Guid processedBy, string userRole, ProcessCancelRequest request)
		{
			// =========================================================
			// 0. LẤY DỮ LIỆU VÀ KIỂM TRA ĐIỀU KIỆN BAN ĐẦU
			// =========================================================
			var cancelRequest = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu hủy đơn.");

			if (cancelRequest.Status != CancelRequestStatus.Pending)
				throw AppException.BadRequest("Yêu cầu này đã được xử lý trước đó.");

			if (cancelRequest.IsRefundRequired && userRole != UserRole.admin.ToString())
				throw AppException.Forbidden("Chỉ Quản trị viên mới có thể duyệt yêu cầu hủy đơn có hoàn tiền.");

			var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
			  ?? throw AppException.NotFound("Không tìm thấy đơn hàng liên quan.");

			PaymentTransaction? originalPayment = null;
			PaymentTransaction? pendingRefundPayment = null;
			decimal? finalRefundAmount = null;
			bool isRefundSuccess = false;
			string? refundTransactionNo = null;
			string? refundMessage = null;

			// =========================================================
			// 1. CHUẨN BỊ LOGIC HOÀN TIỀN VÀ PHASE 1 (GHI NHẬN Ý ĐỊNH)
			// =========================================================
			if (request.IsApproved && cancelRequest.IsRefundRequired)
			{
				var refundMethod = request.RefundMethod
					   ?? throw AppException.BadRequest("Bắt buộc chọn phương thức hoàn tiền khi cần hoàn tiền.");

				var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
					p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success))
					.OrderByDescending(p => p.CreatedAt).ToList();

				if (refundMethod == PaymentMethod.ExternalBankTransfer || refundMethod == PaymentMethod.CashInStore)
				{
					originalPayment = successfulOnlinePayments.FirstOrDefault()
						?? throw AppException.NotFound("Không tìm thấy giao dịch thanh toán thành công của đơn hàng để đối chiếu hoàn tiền thủ công.");
				}
				else
				{
					originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == refundMethod)
						?? throw AppException.NotFound($"Không tìm thấy giao dịch {refundMethod} thành công cho đơn hàng này.");
				}

				finalRefundAmount = ResolveRefundAmount(cancelRequest, order, originalPayment.Amount);

				// PHASE 1: Ghi nhận giao dịch hoàn tiền vào DB với trạng thái Pending
				await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					pendingRefundPayment = PaymentTransaction.CreateRefund(
						orderId: order.Id,
						originalPaymentId: originalPayment.Id,
						method: refundMethod,
						refundAmount: finalRefundAmount.Value
					);

					await _unitOfWork.Payments.AddAsync(pendingRefundPayment);
					return true;
				});

				// =========================================================
				// 2. PHASE 2: GỌI 3RD PARTY (NGOÀI TRANSACTION)
				// =========================================================
				var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext hiện không khả dụng.");

				switch (refundMethod)
				{
					case PaymentMethod.VnPay:
						var vnPayResponse = await _vnPayService.RefundAsync(context, new VnPayRefundRequest
						{
							OrderId = order.Id,
							Amount = finalRefundAmount.Value,
							PaymentId = originalPayment.Id,
							TransactionType = finalRefundAmount.Value == originalPayment.Amount ? "02" : "03",
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
						var momoResponse = await _momoService.RefundAsync(context, new MomoRefundRequest
						{
							OrderId = order.Id,
							OrderCode = order.Code,
							Amount = finalRefundAmount.Value,
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
						if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
							throw AppException.BadRequest("Bắt buộc nhập mã tham chiếu giao dịch thủ công cho hoàn tiền chuyển khoản.");

						isRefundSuccess = true;
						refundMessage = request.StaffNote ?? "Đã ghi nhận hoàn tiền thủ công bởi Quản trị viên.";
						refundTransactionNo = request.ManualTransactionReference.Trim();
						break;

					default:
						throw AppException.BadRequest($"Không hỗ trợ hoàn tiền cho phương thức thanh toán {refundMethod}.");
				}

				// PHASE LỖI: NẾU GỌI API LỖI, CẬP NHẬT GIAO DỊCH THÀNH FAILED VÀ CHẶN LUỒNG
				if (!isRefundSuccess && pendingRefundPayment != null)
				{
					await _unitOfWork.ExecuteInTransactionAsync(async () =>
					{
						pendingRefundPayment.MarkFailed(reason: refundMessage, gatewayTransactionNo: refundTransactionNo);
						_unitOfWork.Payments.Update(pendingRefundPayment);
						return true;
					});
					throw AppException.BadRequest($"Hoàn tiền qua {refundMethod} thất bại. Đã hủy xử lý yêu cầu hủy đơn. Lý do: {refundMessage}");
				}
			}

			// =========================================================
			// 3. PHASE 3: CHỐT KẾT QUẢ VÀ CẬP NHẬT TRẠNG THÁI ORDER (TRANSACTION 2)
			// =========================================================
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				// Duyệt / Từ chối Cancel Request
				cancelRequest.Process(processedBy, request.IsApproved, request.StaffNote);

				if (request.IsApproved)
				{
					if (cancelRequest.IsRefundRequired && pendingRefundPayment != null)
					{
						pendingRefundPayment.MarkSuccess(refundTransactionNo);
						_unitOfWork.Payments.Update(pendingRefundPayment);

						cancelRequest.MarkRefunded(refundTransactionNo);
						order.MarkRefunded();
					}

					// Hủy Order và Giao vận
					order.SetStatus(OrderStatus.Cancelled);
					_unitOfWork.Orders.Update(order);

					if (order.ForwardShipping != null)
					{
						order.ForwardShipping.Cancel();
						_unitOfWork.ShippingInfos.Update(order.ForwardShipping);
					}

					// Nhả tồn kho & Hoàn Voucher
					await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(order.Id);

					if (order.UserVoucherId.HasValue)
						await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);
				}

				_unitOfWork.OrderCancelRequests.Update(cancelRequest);

				return BaseResponse<string>.Ok(request.IsApproved ? "Yêu cầu hủy đơn đã được chấp thuận và xử lý." : "Yêu cầu hủy đơn đã bị từ chối.");
			});

			// =========================================================
			// 4. BACKGROUND JOBS BÊN NGOÀI TRANSACTION
			// =========================================================
			if (request.IsApproved && !string.IsNullOrWhiteSpace(order.ForwardShipping?.TrackingNumber))
			{
				_backgroundJobService.EnqueueCancelShippingOrder(_logger, order.ForwardShipping.TrackingNumber);
			}

			_backgroundJobService.EnqueueCustomerNotificationWithFcm(
				_logger,
				cancelRequest.RequestedById,
				"Kết quả yêu cầu hủy đơn",
				$"Yêu cầu hủy đơn #{order.Code} của bạn đã được {(request.IsApproved ? "chấp thuận" : "từ chối")}.",
				request.IsApproved ? NotificationType.Success : NotificationType.Warning,
				cancelRequest.Id,
				NotifiReferecneType.OrderCancelRequest);

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