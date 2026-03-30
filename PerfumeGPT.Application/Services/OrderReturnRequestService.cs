using Microsoft.AspNetCore.Http;
using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderReturnRequestService : IOrderReturnRequestService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVnPayService _vnPayService; // Refund via VNPay API not working, just assume it works and return success response, then update refund status in database
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly MediaBulkActionHelper _mediaBulkActionHelper;

		public OrderReturnRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor,
			MediaBulkActionHelper mediaBulkActionHelper)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
			_mediaBulkActionHelper = mediaBulkActionHelper;
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedReturnRequestsAsync(GetPagedReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedResponsesAsync(request);

			return BaseResponse<PagedResult<OrderReturnRequestResponse>>.Ok(
				new PagedResult<OrderReturnRequestResponse>(items, request.PageNumber, request.PageSize, totalCount),
				"Return requests retrieved successfully.");
		}

		public async Task<BaseResponse<OrderReturnRequestResponse>> GetReturnRequestByIdAsync(Guid requestId, Guid requesterId, bool isPrivilegedUser)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetResponseByIdAsync(requestId)
				?? throw AppException.NotFound("Return request not found.");

			if (!isPrivilegedUser && returnRequest.CustomerId != requesterId)
				throw AppException.Forbidden("You are not allowed to view this return request.");

			return BaseResponse<OrderReturnRequestResponse>.Ok(returnRequest, "Return request retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateReturnRequestAsync(Guid customerId, CreateReturnRequestDto request)
		{
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(request.OrderId)
					  ?? throw AppException.NotFound("Order not found.");

				if (order.CustomerId != customerId)
					throw AppException.Forbidden("You are not allowed to create return request for this order.");

				if (order.Status != OrderStatus.Delivered)
					throw AppException.BadRequest("Only delivered orders can be returned.");

				var hasOpenRequest = await _unitOfWork.OrderReturnRequests.AnyAsync(r =>
					r.OrderId == order.Id
					&& r.Status != ReturnRequestStatus.Rejected
					&& r.Status != ReturnRequestStatus.Completed);

				if (hasOpenRequest)
					throw AppException.Conflict("This order already has an active return request.");

				if (request.ReturnItems == null || request.ReturnItems.Count == 0)
					throw AppException.BadRequest("At least one return item is required.");

				var duplicateOrderDetailIds = request.ReturnItems
					.GroupBy(x => x.OrderDetailId)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateOrderDetailIds.Count != 0)
					throw AppException.BadRequest("Duplicate order detail IDs in return items are not allowed.");

				var orderDetailMap = order.OrderDetails.ToDictionary(x => x.Id, x => x);
				var maxRequestableRefund = 0m;
				var itemsToReturn = new List<(Guid OrderDetailId, int ReturnedQuantity)>();

				foreach (var item in request.ReturnItems)
				{
					if (!orderDetailMap.TryGetValue(item.OrderDetailId, out var orderDetail))
						throw AppException.BadRequest($"Order detail {item.OrderDetailId} does not belong to this order.");

					if (item.ReturnedQuantity <= 0)
						throw AppException.BadRequest("Returned quantity must be greater than 0.");

					if (item.ReturnedQuantity > orderDetail.Quantity)
						throw AppException.BadRequest($"Returned quantity for order detail {item.OrderDetailId} exceeds purchased quantity.");

					maxRequestableRefund += orderDetail.UnitPrice * item.ReturnedQuantity;
					itemsToReturn.Add((item.OrderDetailId, item.ReturnedQuantity));
				}

				if (request.RequestedRefundAmount > maxRequestableRefund)
					throw AppException.BadRequest("Requested refund amount exceeds maximum refundable amount for selected return items.");

				var returnRequest = OrderReturnRequest.Create(
					request.OrderId,
					customerId,
					request.Reason,
					request.RequestedRefundAmount,
					itemsToReturn,
				   request.CustomerNote
					);

				await _unitOfWork.OrderReturnRequests.AddAsync(returnRequest);

				return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Return request created successfully.");
			});

			if (request.TemporaryMediaIds == null || request.TemporaryMediaIds.Count == 0)
				return response;

			if (!Guid.TryParse(response.Payload, out var returnRequestId))
				throw AppException.Internal("Failed to parse return request ID.");

			var conversionResult = await _mediaBulkActionHelper.ConvertTemporaryMediaToPermanentAsync(
				request.TemporaryMediaIds,
				EntityType.OrderReturnRequest,
				returnRequestId);

			if (conversionResult.TotalProcessed == 0)
				return BaseResponse<string>.Ok(returnRequestId.ToString(), "Return request created successfully.");

			var message = conversionResult.HasError
				? $"Return request created successfully with {conversionResult.FailedItems.Count} proof image upload failure(s)."
				: "Return request created successfully.";

			return BaseResponse<string>.Ok(returnRequestId.ToString(), message);
		}

		public async Task<BaseResponse<string>> ProcessInitialRequestAsync(Guid processedById, Guid requestId, ProcessInitialReturnDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				returnRequest.Process(processedById, request.IsApproved, request.StaffNote);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok(
					request.IsApproved ? "Return request approved for shipment." : "Return request rejected.");
			});
		}

		public async Task<BaseResponse<string>> StartInspectionAsync(Guid inspectedById, Guid requestId, StartInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				returnRequest.StartInspection(inspectedById, request.InspectionNote);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Inspection started.");
			});
		}

		public async Task<BaseResponse<string>> RecordInspectionResultAsync(Guid inspectedById, Guid requestId, RecordInspectionDto request)
		{
			if (request.InspectionResults == null || request.InspectionResults.Count == 0)
				throw AppException.BadRequest("Inspection results are required.");

			var duplicateDetailIds = request.InspectionResults
				.GroupBy(x => x.DetailId)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicateDetailIds.Count != 0)
				throw AppException.BadRequest("Duplicate return detail IDs in inspection results are not allowed.");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				if (returnRequest.InspectedById != inspectedById)
					throw AppException.Forbidden("Only the assigned inspector can record inspection result.");

				var requestDetailIds = returnRequest.ReturnDetails.Select(d => d.Id).ToHashSet();
				var submittedDetailIds = request.InspectionResults.Select(x => x.DetailId).ToHashSet();

				if (!requestDetailIds.SetEquals(submittedDetailIds))
					throw AppException.BadRequest("Inspection results must include all and only return details in the request.");

				var inspectionResults = request.InspectionResults
					.Select(x => (x.DetailId, x.IsRestocked, x.Note));

				returnRequest.RecordInspectionResult(request.ApprovedRefundAmount, inspectionResults);

				var restockedDetails = returnRequest.ReturnDetails.Where(d => d.IsRestocked == true).ToList();

				if (restockedDetails.Count > 0)
				{
					var committedReservations = (await _unitOfWork.StockReservations.GetByOrderIdAsync(returnRequest.OrderId))
						.Where(r => r.Status == ReservationStatus.Committed)
						.ToList();

					if (committedReservations.Count == 0)
						throw AppException.BadRequest("No committed stock reservations found for this order to restock.");

					var stockAdjustment = StockAdjustment.Create(
						inspectedById,
						DateTime.UtcNow,
						StockAdjustmentReason.Return,
						$"Auto restock from return request {returnRequest.Id}");

					var restockedByVariant = restockedDetails
						.GroupBy(d => d.OrderDetail.VariantId)
						.ToDictionary(g => g.Key, g => g.Sum(x => x.ReturnedQuantity));

					foreach (var item in restockedByVariant)
					{
						var variantReservations = committedReservations
							.Where(r => r.VariantId == item.Key)
							.ToList();

						var committedQty = variantReservations.Sum(r => r.ReservedQuantity);
						if (committedQty < item.Value)
							throw AppException.BadRequest($"Committed reservations for variant {item.Key} are not enough to restock returned quantity.");

						var remainingToRestock = item.Value;
						foreach (var reservation in variantReservations)
						{
							if (remainingToRestock <= 0)
								break;

							var qtyFromThisReservation = Math.Min(remainingToRestock, reservation.ReservedQuantity);
							var note = $"Restock from return request {returnRequest.Id}";

							reservation.Batch.IncreaseQuantity(qtyFromThisReservation);
							_unitOfWork.Batches.Update(reservation.Batch);

							stockAdjustment.AddApprovedDetail(item.Key,
								reservation.BatchId,
								qtyFromThisReservation,
								qtyFromThisReservation,
								note);

							remainingToRestock -= qtyFromThisReservation;
						}

						var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == item.Key)
							?? throw AppException.NotFound($"Stock for variant {item.Key} not found.");

						stock.Increase(item.Value);
						_unitOfWork.Stocks.Update(stock);
					}

					stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					stockAdjustment.Complete(inspectedById);
					await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
				}
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Inspection result recorded.");
			});
		}

		public async Task<BaseResponse<string>> RejectAfterInspectionAsync(Guid inspectedById, Guid requestId, RejectInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				returnRequest.RejectAfterInspection(inspectedById, request.Note);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Return request rejected after inspection.");
			});
		}

		public async Task<BaseResponse<string>> ProcessRefundAsync(Guid financeAdminId, Guid requestId)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
				?? throw AppException.NotFound("Return request not found.");

			if (returnRequest.Status != ReturnRequestStatus.ReadyForRefund)
				throw AppException.BadRequest("Return request is not ready for refund.");

			if (!returnRequest.ApprovedRefundAmount.HasValue || returnRequest.ApprovedRefundAmount.Value <= 0)
				throw AppException.BadRequest("Approved refund amount must be greater than 0.");

			var payment = (await _unitOfWork.Payments.GetAllAsync(
				p => p.OrderId == returnRequest.OrderId
					&& p.TransactionStatus == TransactionStatus.Success
					&& p.Method == PaymentMethod.VnPay))
				.OrderByDescending(p => p.CreatedAt)
				.FirstOrDefault()
				?? throw AppException.NotFound("No successful VNPay payment found for this order.");

			var httpContext = _httpContextAccessor.HttpContext
				?? throw AppException.Internal("HttpContext not available.");

			var refundResponse = await _vnPayService.RefundAsync(httpContext, new VnPayRefundRequest
			{
				OrderId = returnRequest.OrderId,
				Amount = returnRequest.ApprovedRefundAmount.Value,
				PaymentId = payment.Id,
				TransactionType = returnRequest.ApprovedRefundAmount.Value == payment.Amount ? "02" : "03",
				CreateBy = financeAdminId.ToString(),
				OrderInfo = $"Refund for return request {requestId}",
				TransactionDate = payment.CreatedAt.ToString("yyyyMMddHHmmss")
			});

			if (!refundResponse.IsSuccess)
				throw AppException.BadRequest($"Refund failed via VNPay: {refundResponse.Message}");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var freshReturnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
					?? throw AppException.NotFound("Return request not found during transaction.");

				if (freshReturnRequest.Status != ReturnRequestStatus.ReadyForRefund)
					throw AppException.BadRequest("Return request status changed and is no longer refundable.");

				var order = freshReturnRequest.Order;

				freshReturnRequest.MarkRefunded(refundResponse.TransactionNo);
				order.MarkRefunded();

				if (order.Status != OrderStatus.Returned)
				{
					order.SetStatus(OrderStatus.Returned);
				}

				_unitOfWork.OrderReturnRequests.Update(freshReturnRequest);
				_unitOfWork.Orders.Update(order);

				return BaseResponse<string>.Ok("Refund processed and return request completed.");
			});
		}
	}
}
