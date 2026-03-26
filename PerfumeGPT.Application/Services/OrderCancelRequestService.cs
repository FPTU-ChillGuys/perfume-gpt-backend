using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
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

		public async Task<BaseResponse<string>> ProcessRequestAsync(Guid requestId, Guid processedBy, ProcessCancelRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var cancelRequest = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId)
					  ?? throw AppException.NotFound("Cancel request not found.");

				cancelRequest.Process(processedBy, request.IsApproved, request.StaffNote);

				if (request.IsApproved)
				{
					var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId) ?? throw AppException.NotFound("Associated order not found.");
					if (cancelRequest.IsRefundRequired)
					{
						// Attempt VM refund if possible
						var payment = (await _unitOfWork.Payments.GetAllAsync(
							p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success))
							.OrderByDescending(p => p.CreatedAt)
							.FirstOrDefault();

						if (payment != null && payment.Method == PaymentMethod.VnPay)
						{
							var context = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext not available.");

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

							cancelRequest.MarkRefunded();
							order.MarkRefunded();
						}
						else
						{
							// If COD or other methods that require manual transfer
							cancelRequest.MarkRefunded();
							order.MarkRefunded();
						}
					}

					// Process cancellation updates
					order.SetStatus(OrderStatus.Canceled);
					_unitOfWork.Orders.Update(order);

					if (order.ShippingInfo != null)
					{
						order.ShippingInfo.Cancel();
						_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
					}

					// Release stock reservation if online
					if (order.Type == OrderType.Online)
					{
						await _stockReservationService.ReleaseReservationAsync(order.Id);
					}

					// Release voucher if used
					if (order.UserVoucherId.HasValue && order.CustomerId.HasValue)
					{
						var userVoucher = order.UserVoucher ?? await _unitOfWork.UserVouchers.GetByIdAsync(order.UserVoucherId.Value);
						if (userVoucher != null)
						{
							await _voucherService.ReleaseReservedVoucherAsync(order.Id);
						}
					}
				}

				_unitOfWork.OrderCancelRequests.Update(cancelRequest);

				return BaseResponse<string>.Ok(request.IsApproved ? "Cancel request approved." : "Cancel request rejected.");
			});
		}
	}
}
