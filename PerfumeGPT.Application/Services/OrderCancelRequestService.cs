using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
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

		public OrderCancelRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor,
			IStockReservationService stockReservationService,
			IVoucherService voucherService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
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

			if (cancelRequest.IsRefundRequired && userRole != "Admin")
			{
				throw AppException.Forbidden("Only Administrators can approve cancellation requests that require a refund.");
			}

			VnPayRefundResponse? vnPayResponse = null;
			var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId)
				?? throw AppException.NotFound("Associated order not found.");

			if (request.IsApproved && cancelRequest.IsRefundRequired)
			{
				var payment = (await _unitOfWork.Payments.GetAllAsync(
							p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success && p.Method == PaymentMethod.VnPay))
							.OrderByDescending(p => p.CreatedAt)
							.FirstOrDefault();

				if (payment != null)
				{
					// Gọi VNPay ở đây. DB hoàn toàn thảnh thơi không bị khóa.
					//var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext not available.");
					//vnPayResponse = await _vnPayService.RefundAsync(context, new VnPayRefundRequest { ... });

					//if (!vnPayResponse.IsSuccess)
					//{
					//	// Nếu VNPay thất bại, ta dừng luôn quá trình duyệt đơn hủy
					//	throw AppException.BadRequest($"Refund failed via VNPay: {vnPayResponse.Message}. Cancellation aborted.");
					//}


					//var refundReq = new VnPayRefundRequest
					//{
					//	OrderId = order.Id,
					//	Amount = cancelRequest.RefundAmount ?? payment.Amount,
					//	PaymentId = payment.Id,
					//	TransactionType = "02", // full refund
					//	CreateBy = processedBy.ToString(),
					//	OrderInfo = $"Refund for Order {order.Id}",
					//};

					//var refundRes = await _vnPayService.RefundAsync(context, refundReq);

					//if (refundRes.IsSuccess)
					//{
					//	cancelRequest.IsRefunded = true;
					//	cancelRequest.VnpTransactionNo = refundRes.TransactionNo;
					//	order.PaymentStatus = PaymentStatus.Refunded;
					//}
					//else
					//{
					//	// We might still consider it approved, but log refund failure
					//	cancelRequest.StaffNote += $" | Refund failed: {refundRes.Message}";
					//}
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
					if (freshCancelReq.IsRefundRequired)
					{
						freshCancelReq.MarkRefunded(vnPayResponse?.TransactionNo);
						freshOrder.MarkRefunded();
					}

					freshOrder.SetStatus(OrderStatus.Cancelled);
					_unitOfWork.Orders.Update(freshOrder);

					if (freshOrder.ShippingInfo != null)
					{
						freshOrder.ShippingInfo.Cancel();
						_unitOfWork.ShippingInfos.Update(freshOrder.ShippingInfo);
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
