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

		public OrderCancelRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
		   IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			IStockReservationService stockReservationService,
			IVoucherService voucherService,
			IGHNService ghnService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
			_ghnService = ghnService;
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

		private static string? MaskAccountNumber(string? accountNumber)
		{
			if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
				return accountNumber;

			return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
		}

		public async Task<BaseResponse<string>> ProcessRequestAsync(Guid requestId, Guid processedBy, string userRole, ProcessCancelRequest request)
		{
			var cancelRequest = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Cancel request not found.");

			if (cancelRequest.Status != CancelRequestStatus.Pending)
				throw AppException.BadRequest("This request has already been processed.");

			if (cancelRequest.IsRefundRequired && userRole != UserRole.admin.ToString())
			{
				throw AppException.Forbidden("Only Administrators can approve cancellation requests that require a refund.");
			}

			string? refundTransactionNo = null;
			string? refundMessage = null;
			PaymentTransaction? originalPayment = null;

			var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
				?? throw AppException.NotFound("Associated order not found.");

			if (request.IsApproved)
			{
				if (cancelRequest.IsRefundRequired)
				{
					var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
						p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success))
						.OrderByDescending(p => p.CreatedAt).ToList();

					originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod) ?? throw AppException.NotFound($"No successful {request.RefundMethod} payment found for this order.");

					var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext not available.");
					var refundAmount = cancelRequest.RefundAmount ?? originalPayment.Amount;
					var isRefundSuccess = false;

					switch (originalPayment.Method)
					{
						case PaymentMethod.VnPay:
							var vnPayResponse = await _vnPayService.RefundAsync(context, new VnPayRefundRequest
							{
								OrderId = order.Id,
								Amount = refundAmount,
								PaymentId = originalPayment.Id,
								TransactionType = refundAmount == originalPayment.Amount ? "02" : "03",
								TransactionNo = originalPayment.GatewayTransactionNo,
								CreateBy = processedBy.ToString(),
								OrderInfo = $"Refund for Order {order.Code}",
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
								Amount = refundAmount,
								PaymentId = originalPayment.Id,
								TransactionNo = originalPayment.GatewayTransactionNo,
								Description = $"Refund for Order {order.Code}"
							});

							isRefundSuccess = momoResponse.IsSuccess;
							refundMessage = momoResponse.Message;
							refundTransactionNo = momoResponse.TransactionNo;
							break;

						case PaymentMethod.CashInStore:
						case PaymentMethod.ExternalBankTransfer:
							if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
								throw AppException.BadRequest("Manual transaction reference is required for Bank Transfer refunds.");

							isRefundSuccess = true;
							refundMessage = request.StaffNote ?? "Manual refund recorded.";
							refundTransactionNo = request.ManualTransactionReference?.Trim();
							break;

						default:
							throw AppException.BadRequest($"Refund is not supported for payment method {originalPayment.Method}.");
					}

					if (!isRefundSuccess)
					{
						var failedRefund = PaymentTransaction.CreateRefund(
							orderId: order.Id,
							originalPaymentId: originalPayment.Id,
							method: originalPayment.Method,
							refundAmount: refundAmount
						);

						failedRefund.MarkFailed(
						 reason: refundMessage,
							gatewayTransactionNo: refundTransactionNo
						);

						await _unitOfWork.Payments.AddAsync(failedRefund);
						await _unitOfWork.SaveChangesAsync();

						throw AppException.BadRequest($"Refund failed via {originalPayment.Method}. Cancellation aborted. Reason: {refundMessage}");
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
						throw AppException.BadRequest($"Failed to cancel shipping order on GHN: {ex.Message}");
					}
				}
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var freshCancelReq = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Cancel request not found during transaction.");

				var freshOrder = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
					?? throw AppException.NotFound("Associated order not found during transaction.");

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
							refundAmount: cancelRequest.RefundAmount ?? originalPayment.Amount
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

				return BaseResponse<string>.Ok(request.IsApproved ? "Cancel request approved and processed." : "Cancel request rejected.");
			});
		}
	}
}
