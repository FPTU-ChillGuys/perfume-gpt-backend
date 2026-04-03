using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
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
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IStockReservationService _stockReservationService;
		private readonly IVoucherService _voucherService;
		private readonly IGHNService _ghnService;

		public OrderCancelRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor,
			IStockReservationService stockReservationService,
			IVoucherService voucherService,
			IGHNService ghnService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
			_ghnService = ghnService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedRequestsAsync(GetPagedCancelRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderCancelRequests.GetPagedResponsesAsync(request);

			return BaseResponse<PagedResult<OrderCancelRequestResponse>>.Ok(
				new PagedResult<OrderCancelRequestResponse>(items, request.PageNumber, request.PageSize, totalCount)
			);
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

			VnPayRefundResponse? vnPayResponse = null;
			PaymentTransaction? originalPayment = null;

			var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
				?? throw AppException.NotFound("Associated order not found.");

			if (request.IsApproved)
			{
				if (cancelRequest.IsRefundRequired)
				{
					originalPayment = (await _unitOfWork.Payments.GetAllAsync(
								p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success && p.Method == PaymentMethod.VnPay))
								.OrderByDescending(p => p.CreatedAt)
								.FirstOrDefault()
								?? throw AppException.NotFound("No successful VNPay payment found for this order.");

					var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext not available.");
					var refundReq = new VnPayRefundRequest
					{
						OrderId = order.Id,
						Amount = cancelRequest.RefundAmount ?? originalPayment.Amount,
						PaymentId = originalPayment.Id,
						TransactionType = "02",
						TransactionNo = originalPayment.GatewayTransactionNo,
						CreateBy = processedBy.ToString(),
						OrderInfo = $"Refund for Order {order.Id}",
						TransactionDate = originalPayment.CreatedAt.ToString("yyyyMMddHHmmss")
					};

					vnPayResponse = await _vnPayService.RefundAsync(context, refundReq);

					if (vnPayResponse == null || !vnPayResponse.IsSuccess)
					{
						var failedRefund = PaymentTransaction.CreateRefund(
							orderId: order.Id,
							originalPaymentId: originalPayment.Id,
							method: PaymentMethod.VnPay,
							refundAmount: cancelRequest.RefundAmount ?? 0
						);

						failedRefund.MarkFailed(
							reason: vnPayResponse?.Message ?? "No response from VNPay",
							gatewayTransactionNo: vnPayResponse?.TransactionNo
						);

						await _unitOfWork.Payments.AddAsync(failedRefund);
						await _unitOfWork.SaveChangesAsync();

						throw AppException.BadRequest($"Refund failed via VNPay. Cancellation aborted. Reason: {vnPayResponse?.Message}");
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
						freshCancelReq.MarkRefunded(vnPayResponse?.TransactionNo);
						freshOrder.MarkRefunded();

						var refundPayment = PaymentTransaction.CreateRefund(
							orderId: freshOrder.Id,
							originalPaymentId: originalPayment.Id,
							method: PaymentMethod.VnPay,
							refundAmount: cancelRequest.RefundAmount ?? 0
						);

						refundPayment.MarkSuccess(vnPayResponse!.TransactionNo);

						await _unitOfWork.Payments.AddAsync(refundPayment);
					}

					freshOrder.SetStatus(OrderStatus.Cancelled);
					_unitOfWork.Orders.Update(freshOrder);

					if (freshOrder.ForwardShipping != null)
					{
						freshOrder.ForwardShipping.Cancel();
						_unitOfWork.ShippingInfos.Update(freshOrder.ForwardShipping);
					}

					if (freshOrder.Type == OrderType.Online)
						await _stockReservationService.ReleaseReservationAsync(freshOrder.Id);

					if (freshOrder.UserVoucherId.HasValue)
						await _voucherService.RefundVoucherForCancelledOrderAsync(freshOrder.Id);
				}

				_unitOfWork.OrderCancelRequests.Update(freshCancelReq);

				return BaseResponse<string>.Ok(request.IsApproved ? "Cancel request approved and processed." : "Cancel request rejected.");
			});
		}
	}
}
