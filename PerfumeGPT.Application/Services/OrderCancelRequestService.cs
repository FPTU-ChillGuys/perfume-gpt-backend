using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderCancelRequestService : IOrderCancelRequestService
	{
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

		public async Task<BaseResponse<PagedResult<OrderCancelRequestResponse>>> GetPagedRequestsAsync(GetPagedCancelRequestsRequest request)
		{
			try
			{
				var query = await _unitOfWork.OrderCancelRequests.GetAllAsync(
					filter: request.Status.HasValue ? r => r.Status == request.Status.Value : null,
					include: q => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(q, r => r.RequestedBy),
					orderBy: q => q.OrderByDescending(r => r.CreatedAt),
					asNoTracking: true
				);

				var totalCount = query.Count();
				var pagedData = query
					.Skip((request.PageNumber - 1) * request.PageSize)
					.Take(request.PageSize)
					.Select(r => new OrderCancelRequestResponse
					{
						Id = r.Id,
						OrderId = r.OrderId,
						RequestedById = r.RequestedById,
						RequestedByEmail = r.RequestedBy != null ? r.RequestedBy.Email : null,
						ProcessedById = r.ProcessedById,
						Reason = r.Reason,
						StaffNote = r.StaffNote,
						Status = r.Status,
						IsRefundRequired = r.IsRefundRequired,
						RefundAmount = r.RefundAmount,
						IsRefunded = r.IsRefunded,
						VnpTransactionNo = r.VnpTransactionNo,
						CreatedAt = r.CreatedAt,
						UpdatedAt = r.UpdatedAt
					})
					.ToList();

				return BaseResponse<PagedResult<OrderCancelRequestResponse>>.Ok(
					new PagedResult<OrderCancelRequestResponse>(pagedData, request.PageNumber, request.PageSize, totalCount)
				);
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<OrderCancelRequestResponse>>.Fail($"Error retrieving cancel requests: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> ProcessRequestAsync(Guid requestId, Guid processedBy, ProcessCancelRequestDto request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var cancelRequest = await _unitOfWork.OrderCancelRequests.GetByIdAsync(requestId);
					if (cancelRequest == null)
						return BaseResponse<string>.Fail("Cancel request not found.", ResponseErrorType.NotFound);

					if (cancelRequest.Status != CancelRequestStatus.Pending)
						return BaseResponse<string>.Fail("Cancel request is not pending.", ResponseErrorType.BadRequest);

					cancelRequest.ProcessedById = processedBy;
					cancelRequest.StaffNote = request.StaffNote;
					cancelRequest.Status = request.IsApproved ? CancelRequestStatus.Approved : CancelRequestStatus.Rejected;

					if (request.IsApproved)
					{
						var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(cancelRequest.OrderId);
						if (order == null)
							return BaseResponse<string>.Fail("Associated order not found.", ResponseErrorType.NotFound);

						if (cancelRequest.IsRefundRequired)
						{
							// Attempt VM refund if possible
							var payment = (await _unitOfWork.Payments.GetAllAsync(
								p => p.OrderId == order.Id && p.TransactionStatus == TransactionStatus.Success))
								.OrderByDescending(p => p.CreatedAt)
								.FirstOrDefault();

							if (payment != null && payment.Method == PaymentMethod.VnPay)
							{
								var context = _httpContextAccessor.HttpContext;
								if (context == null)
									return BaseResponse<string>.Fail("HttpContext not available.", ResponseErrorType.InternalError);

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

								cancelRequest.IsRefunded = true;
								order.PaymentStatus = PaymentStatus.Refunded;
							}
							else
							{
								// If COD or other methods that require manual transfer
								cancelRequest.IsRefunded = true;
								order.PaymentStatus = PaymentStatus.Refunded;
							}
						}

						// Process cancellation updates
						order.Status = OrderStatus.Canceled;
						_unitOfWork.Orders.Update(order);

						if (order.ShippingInfo != null)
						{
							order.ShippingInfo.Status = ShippingStatus.Cancelled;
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
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error processing cancel request: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
