using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.StockAdjustmentDetail;

namespace PerfumeGPT.Application.Services
{
	public class OrderReturnRequestService : IOrderReturnRequestService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVnPayService _vnPayService; // Refund via VNPay API not working, just assume it works and return success response, then update refund status in database
		private readonly IMomoService _momoService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly MediaBulkActionHelper _mediaBulkActionHelper;
		private readonly IOrderShippingHelper _orderShippingHelper;
		private readonly IContactAddressService _recipientService;

		public OrderReturnRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			MediaBulkActionHelper mediaBulkActionHelper,
			IOrderShippingHelper orderShippingHelper,
			IContactAddressService recipientService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_mediaBulkActionHelper = mediaBulkActionHelper;
			_orderShippingHelper = orderShippingHelper;
			_recipientService = recipientService;
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedReturnRequestsAsync(GetPagedReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedResponsesAsync(request);

			return BaseResponse<PagedResult<OrderReturnRequestResponse>>.Ok(
				new PagedResult<OrderReturnRequestResponse>(items, request.PageNumber, request.PageSize, totalCount),
				"Return requests retrieved successfully.");
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedUserReturnRequestsAsync(Guid userId, GetPagedUserReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedUserResponsesAsync(userId, request);

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

				var hasAlreadyReturnedRequest = await _unitOfWork.OrderReturnRequests.AnyAsync(r =>
					r.OrderId == order.Id
					&& r.Status == ReturnRequestStatus.Completed);

				if (hasAlreadyReturnedRequest)
					throw AppException.Conflict("This order has already been returned before. Please contact support for further assistance.");


				var orderDetailsById = order.OrderDetails.ToDictionary(x => x.Id);

				var payloadDetails = request.ReturnItems
					.Select(item =>
					{
						if (!orderDetailsById.TryGetValue(item.OrderDetailId, out var orderDetail))
							throw AppException.BadRequest($"Order detail {item.OrderDetailId} does not belong to this order.");

						if (item.Quantity > orderDetail.Quantity)
							throw AppException.BadRequest($"Returned quantity for order detail {item.OrderDetailId} cannot exceed ordered quantity.");

						return new OrderReturnRequest.ReturnRequestDetailPayload
						{
							OrderDetailId = item.OrderDetailId,
							RequestedQuantity = item.Quantity,
							OrderedQuantity = orderDetail.Quantity
						};
					})
					.ToList();

				var requestedRefundAmount = request.ReturnItems
					.Sum(item =>
					{
						var orderDetail = orderDetailsById[item.OrderDetailId];
						return orderDetail.RefundableUnitPrice * item.Quantity;
					});

				var requestPayload = new OrderReturnRequest.ReturnRequestPayload
				{
					Reason = request.Reason,
					RequestedRefundAmount = requestedRefundAmount,
					IsRefundOnly = request.IsRefundOnly,
					ReturnDetails = payloadDetails,
					CustomerNote = request.CustomerNote
				};

				var pickupAddress = await _recipientService.CreateContactAddressAsync(request.Recipient, request.SavedAddressId, customerId);

				var returnRequest = OrderReturnRequest.Create(request.OrderId, customerId, requestPayload);
				returnRequest.AttachPickupAddress(pickupAddress.Id);
				returnRequest.PickupAddress = pickupAddress;

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

		public async Task<BaseResponse<string>> UpdateReturnRequestAsync(Guid customerId, Guid requestId, UpdateReturnRequestDto request)
		{
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					  ?? throw AppException.NotFound("Return request not found.");

				  if (returnRequest.CustomerId != customerId)
					  throw AppException.Forbidden("You are not allowed to update this return request.");

				  if (request.RemoveMediaIds != null && request.RemoveMediaIds.Count > 0)
				  {
					  var existingMediaIds = returnRequest.ProofImages.Select(x => x.Id).ToHashSet();
					  var invalidMediaId = request.RemoveMediaIds.FirstOrDefault(id => !existingMediaIds.Contains(id));

					  if (invalidMediaId != Guid.Empty)
						  throw AppException.BadRequest($"Media {invalidMediaId} does not belong to this return request.");
				  }

				  returnRequest.UpdateByCustomer(customerId, request.CustomerNote);
				  _unitOfWork.OrderReturnRequests.Update(returnRequest);

				  return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Return request updated and resubmitted for review.");
			  });

			if (request.RemoveMediaIds != null && request.RemoveMediaIds.Count > 0)
			{
				await _mediaBulkActionHelper.DeleteMultipleMediaAsync(request.RemoveMediaIds);
			}

			if (request.TemporaryMediaIds == null || request.TemporaryMediaIds.Count == 0)
				return response;

			if (!Guid.TryParse(response.Payload, out var returnRequestId))
				throw AppException.Internal("Failed to parse return request ID.");

			var conversionResult = await _mediaBulkActionHelper.ConvertTemporaryMediaToPermanentAsync(
				request.TemporaryMediaIds,
				EntityType.OrderReturnRequest,
				returnRequestId);

			if (conversionResult.TotalProcessed == 0)
				return response;

			var message = conversionResult.HasError
				? $"Return request updated with {conversionResult.FailedItems.Count} proof image/video upload failure(s)."
				: "Return request updated and resubmitted for review.";

			return BaseResponse<string>.Ok(returnRequestId.ToString(), message);
		}

		public async Task<BaseResponse<string>> CancelReturnRequestByCustomerAsync(Guid customerId, Guid requestId)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				returnRequest.CancelByCustomer(customerId);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Return request cancelled.");
			});
		}

		public async Task<BaseResponse<string>> ProcessInitialRequestAsync(Guid processedById, Guid requestId, ProcessInitialReturnDto request)
		{
			OrderReturnRequest? requestForGhn = null;
			ContactAddress? contactInfoForGhn = null;

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithPickAddressAsync(requestId)
					 ?? throw AppException.NotFound("Return request not found.");

				returnRequest.Process(processedById, request.IsApproved, request.IsRequestMoreInfo, request.StaffNote);

				if (request.IsApproved && !returnRequest.IsRefundOnly)
				{
					var contactInfo = returnRequest.PickupAddress ?? throw AppException.Internal("Pickup address not found for approved return request.");
					var estimatedDeliveryDate = await _orderShippingHelper.GetLeadTimeAsync(contactInfo.DistrictId, contactInfo.WardCode);

					var returnShipping = ShippingInfo.Create(CarrierName.GHN, ShippingType.Return, 0, estimatedDeliveryDate);
					await _unitOfWork.ShippingInfos.AddAsync(returnShipping);

					returnRequest.AttachReturnShipping(returnShipping.Id);
					returnRequest.ReturnShipping = returnShipping;

					requestForGhn = returnRequest;
					contactInfoForGhn = contactInfo;
				}

				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return true;
			});

			if (request.IsApproved && requestForGhn != null && contactInfoForGhn != null)
			{
				try
				{
					var shippingCreated = await _orderShippingHelper.CreateGHNShippingOrderAsync(requestForGhn, contactInfoForGhn);

					if (!shippingCreated)
					{
						return BaseResponse<string>.Ok(
							"Return request approved locally, BUT failed to create GHN return shipping order. Please check GHN configuration and retry manually.");
					}
				}
				catch (Exception ex)
				{
					return BaseResponse<string>.Ok(
						$"Return request approved locally, BUT GHN API threw an error: {ex.Message}. Please retry sync manually.");
				}
				return BaseResponse<string>.Ok("Return request approved for shipment and GHN order created successfully.");
			}

			if (request.IsApproved)
			{
				return BaseResponse<string>.Ok("Return request approved and moved to refund processing.");
			}

			if (request.IsRequestMoreInfo)
			{
				return BaseResponse<string>.Ok("Return request requires more information from customer.");
			}

			return BaseResponse<string>.Ok("Return request rejected.");
		}

		public async Task<BaseResponse<string>> StartInspectionAsync(Guid inspectedById, Guid requestId, StartInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				bool isArrived = true;

				if (returnRequest.ReturnShipping != null)
				{
					isArrived = returnRequest.ReturnShipping.Status == ShippingStatus.Delivered;
				}

				if (!isArrived)
				{
					throw AppException.BadRequest("Return package has not been delivered to the store yet. Cannot start inspection.");
				}

				returnRequest.StartInspection(inspectedById, request.InspectionNote);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				if (returnRequest.Order.Status == OrderStatus.Delivered ||
					returnRequest.Order.Status == OrderStatus.Partial_Returned)
				{
					returnRequest.Order.SetStatus(OrderStatus.Returning);
				}

				return BaseResponse<string>.Ok("Inspection started.");
			});
		}

		public async Task<BaseResponse<string>> RecordInspectionResultAsync(Guid inspectedById, Guid requestId, RecordInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					?? throw AppException.NotFound("Return request not found.");

				if (returnRequest.InspectedById != inspectedById)
					throw AppException.Forbidden("Only the assigned inspector can record inspection result.");

				returnRequest.RecordInspectionResult(
					 request.ApprovedRefundAmount,
					 request.IsRestocked,
					 request.InspectionNote);

				if (returnRequest.IsRestocked)
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

					var orderDetailsById = returnRequest.Order.OrderDetails
						  .ToDictionary(d => d.Id);

					var restockedByVariant = returnRequest.ReturnDetails.Count > 0
						? returnRequest.ReturnDetails
							.Select(d =>
							{
								if (!orderDetailsById.TryGetValue(d.OrderDetailId, out var orderDetail))
									throw AppException.BadRequest($"Order detail {d.OrderDetailId} not found in the order for restocking.");

								return new
								{
									orderDetail.VariantId,
									d.RequestedQuantity
								};
							})
							.GroupBy(x => x.VariantId)
							.ToDictionary(g => g.Key, g => g.Sum(x => x.RequestedQuantity))
						: returnRequest.Order.OrderDetails
							.GroupBy(d => d.VariantId)
							.ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

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

							var detailPayload = new StockAdjustmentDetailPayload
							{
								ProductVariantId = item.Key,
								BatchId = reservation.BatchId,
								AdjustmentQuantity = qtyFromThisReservation,
								Note = note
							};

							stockAdjustment.AddApprovedDetail(detailPayload, approvedQuantity: qtyFromThisReservation);

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

		public async Task<BaseResponse<string>> ProcessRefundAsync(Guid financeAdminId, Guid requestId, ProcessRefundRequest request)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
				?? throw AppException.NotFound("Return request not found.");

			if (returnRequest.Status != ReturnRequestStatus.ReadyForRefund)
				throw AppException.BadRequest("Return request is not ready for refund.");

			if (!returnRequest.ApprovedRefundAmount.HasValue || returnRequest.ApprovedRefundAmount.Value <= 0)
				throw AppException.BadRequest("Approved refund amount must be greater than 0.");

			if (request.RefundMethod != PaymentMethod.VnPay
				 && request.RefundMethod != PaymentMethod.Momo)
			{
				throw AppException.BadRequest("Only VNPay or Momo are supported for refund processing.");
			}

			var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
				p => p.OrderId == returnRequest.OrderId
					&& p.TransactionStatus == TransactionStatus.Success
					&& (p.Method == PaymentMethod.VnPay || p.Method == PaymentMethod.Momo)))
				.OrderByDescending(p => p.CreatedAt)
			   .ToList();

			var payment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
				?? throw AppException.NotFound($"No successful {request.RefundMethod} payment found for this order.");

			var httpContext = _httpContextAccessor.HttpContext
				?? throw AppException.Internal("HttpContext not available.");

			bool isRefundSuccess;
			string refundMessage;
			string? refundTransactionNo;

			switch (payment.Method)
			{
				case PaymentMethod.VnPay:
					var vnPayRefundResponse = await _vnPayService.RefundAsync(httpContext, new VnPayRefundRequest
					{
						OrderId = returnRequest.OrderId,
						Amount = returnRequest.ApprovedRefundAmount.Value,
						PaymentId = payment.Id,
						TransactionNo = payment.GatewayTransactionNo,
						TransactionType = returnRequest.ApprovedRefundAmount.Value == payment.Amount ? "02" : "03",
						CreateBy = financeAdminId.ToString(),
						OrderInfo = $"Refund for return request {requestId}",
						TransactionDate = payment.CreatedAt.ToString("yyyyMMddHHmmss")
					});

					isRefundSuccess = vnPayRefundResponse.IsSuccess;
					refundMessage = vnPayRefundResponse.Message;
					refundTransactionNo = vnPayRefundResponse.TransactionNo;
					break;

				case PaymentMethod.Momo:
					var momoRefundResponse = await _momoService.RefundAsync(httpContext, new MomoRefundRequest
					{
						OrderId = returnRequest.OrderId,
						OrderCode = returnRequest.Order.Code,
						Amount = returnRequest.ApprovedRefundAmount.Value,
						PaymentId = payment.Id,
						TransactionNo = payment.GatewayTransactionNo,
						Description = $"Refund for return request {requestId}"
					});

					isRefundSuccess = momoRefundResponse.IsSuccess;
					refundMessage = momoRefundResponse.Message;
					refundTransactionNo = momoRefundResponse.TransactionNo;
					break;

				default:
					throw AppException.BadRequest($"Refund is not supported for payment method {payment.Method}.");
			}

			if (!isRefundSuccess)
			{
				var failedRefund = PaymentTransaction.CreateRefund(
					orderId: returnRequest.OrderId,
					originalPaymentId: payment.Id,
					method: payment.Method,
					refundAmount: returnRequest.ApprovedRefundAmount.Value
				);

				failedRefund.MarkFailed(refundMessage, refundTransactionNo);

				await _unitOfWork.Payments.AddAsync(failedRefund);

				throw AppException.BadRequest($"Refund failed via {payment.Method}: {refundMessage}");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var freshReturnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
					?? throw AppException.NotFound("Return request not found during transaction.");

				if (freshReturnRequest.Status != ReturnRequestStatus.ReadyForRefund)
					throw AppException.BadRequest("Return request status changed and is no longer refundable.");

				var order = freshReturnRequest.Order;
				var approvedRefundAmount = freshReturnRequest.ApprovedRefundAmount!.Value;

				var isFullyRefunded = approvedRefundAmount >= payment.Amount;

				if (isFullyRefunded)
				{
					order.MarkRefunded();
					if (order.Status != OrderStatus.Returned)
					{
						order.SetStatus(OrderStatus.Returned);
					}
				}
				else
				{
					order.MarkPartiallyRefunded();
					if (order.Status != OrderStatus.Partial_Returned)
					{
						order.SetStatus(OrderStatus.Partial_Returned);
					}
				}

				var successRefund = PaymentTransaction.CreateRefund(
					orderId: order.Id,
					originalPaymentId: payment.Id,
					method: payment.Method,
					refundAmount: approvedRefundAmount
				);

				successRefund.MarkSuccess(refundTransactionNo);

				_unitOfWork.Orders.Update(order);
				await _unitOfWork.Payments.AddAsync(successRefund);

				return BaseResponse<string>.Ok("Refund processed and return request completed.");
			});
		}
	}
}
