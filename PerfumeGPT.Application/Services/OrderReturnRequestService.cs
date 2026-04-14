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
using System.Text.Json;
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
		private readonly INotificationService _notificationService;

		public OrderReturnRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			MediaBulkActionHelper mediaBulkActionHelper,
			IOrderShippingHelper orderShippingHelper,
			IContactAddressService recipientService,
			INotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_mediaBulkActionHelper = mediaBulkActionHelper;
			_orderShippingHelper = orderShippingHelper;
			_recipientService = recipientService;
			_notificationService = notificationService;
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedReturnRequestsAsync(GetPagedReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedResponsesAsync(request);
			items = MaskRefundAccountNumbers(items);

			return BaseResponse<PagedResult<OrderReturnRequestResponse>>.Ok(
				new PagedResult<OrderReturnRequestResponse>(items, request.PageNumber, request.PageSize, totalCount),
				"Return requests retrieved successfully.");
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedUserReturnRequestsAsync(Guid userId, GetPagedUserReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedUserResponsesAsync(userId, request);
			items = MaskRefundAccountNumbers(items);

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

			returnRequest = MaskRefundAccountNumbers(returnRequest);

			return BaseResponse<OrderReturnRequestResponse>.Ok(returnRequest, "Return request retrieved successfully.");
		}

		private List<OrderReturnRequestResponse> MaskRefundAccountNumbers(List<OrderReturnRequestResponse> items)
			=> [.. items.Select(MaskRefundAccountNumbers)];

		private OrderReturnRequestResponse MaskRefundAccountNumbers(OrderReturnRequestResponse item)
		{
			var canViewFullBankInfo = _httpContextAccessor.HttpContext?.User.IsInRole(UserRole.admin.ToString()) == true;
			if (canViewFullBankInfo)
				return item;

			return item with
			{
				RefundAccountNumber = MaskAccountNumber(item.RefundAccountNumber)
			};
		}

		private static string? MaskAccountNumber(string? accountNumber)
		{
			if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length <= 4)
				return accountNumber;

			return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
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

				if (IsFullOrderReturn(orderDetailsById, request.ReturnItems))
				{
					requestedRefundAmount += order.ForwardShipping?.ShippingFee ?? 0m;
				}

				var requestPayload = new OrderReturnRequest.ReturnRequestPayload
				{
					Reason = request.Reason,
					RequestedRefundAmount = requestedRefundAmount,
					IsRefundOnly = request.IsRefundOnly,
					ReturnDetails = payloadDetails,
					CustomerNote = request.CustomerNote,
					RefundBankName = request.RefundBankName,
					RefundAccountNumber = request.RefundAccountNumber,
					RefundAccountName = request.RefundAccountName
				};

				var pickupAddress = await _recipientService.CreateContactAddressAsync(request.Recipient, request.SavedAddressId, customerId);

				var returnRequest = OrderReturnRequest.Create(request.OrderId, customerId, requestPayload);
				returnRequest.AttachPickupAddress(pickupAddress.Id);
				returnRequest.PickupAddress = pickupAddress;

				await _unitOfWork.OrderReturnRequests.AddAsync(returnRequest);

				return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Return request created successfully.");
			});

			if (!Guid.TryParse(response.Payload, out var createdRequestId))
				throw AppException.Internal("Failed to parse return request ID.");

			await _notificationService.SendToRoleAsync(
				UserRole.admin,
				"Yêu cầu trả hàng mới",
				$"Khách hàng đã yêu cầu trả đơn #{request.OrderId}.",
				NotificationType.Warning,
				referenceId: createdRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			await _notificationService.SendToRoleAsync(
				UserRole.staff,
				"Yêu cầu trả hàng mới",
				$"Khách hàng đã yêu cầu trả đơn #{request.OrderId}.",
				NotificationType.Warning,
				referenceId: createdRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			if (request.TemporaryMediaIds == null || request.TemporaryMediaIds.Count == 0)
				return response;

			var conversionResult = await _mediaBulkActionHelper.ConvertTemporaryMediaToPermanentAsync(
				request.TemporaryMediaIds,
				EntityType.OrderReturnRequest,
			   createdRequestId);

			if (conversionResult.TotalProcessed == 0)
				return BaseResponse<string>.Ok(createdRequestId.ToString(), "Return request created successfully.");

			var message = conversionResult.HasError
				? $"Return request created successfully with {conversionResult.FailedItems.Count} proof image upload failure(s)."
				: "Return request created successfully.";

			return BaseResponse<string>.Ok(createdRequestId.ToString(), message);
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

				  returnRequest.UpdateByCustomer(
						 customerId,
						 request.CustomerNote,
						 request.RefundBankName,
						 request.RefundAccountNumber,
						 request.RefundAccountName);
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
			{
				await _notificationService.SendToRoleAsync(
					UserRole.admin,
					"Khách đã bổ sung bằng chứng trả hàng",
					$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{requestId}.",
					NotificationType.Info,
					referenceId: requestId,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				await _notificationService.SendToRoleAsync(
					UserRole.staff,
					"Khách đã bổ sung bằng chứng trả hàng",
					$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{requestId}.",
					NotificationType.Info,
					referenceId: requestId,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return response;
			}

			var message = conversionResult.HasError
				? $"Return request updated with {conversionResult.FailedItems.Count} proof image/video upload failure(s)."
				: "Return request updated and resubmitted for review.";

			await _notificationService.SendToRoleAsync(
				UserRole.admin,
				"Khách đã bổ sung bằng chứng trả hàng",
				$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{returnRequestId}.",
				NotificationType.Info,
				referenceId: returnRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			await _notificationService.SendToRoleAsync(
				UserRole.staff,
				"Khách đã bổ sung bằng chứng trả hàng",
				$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{returnRequestId}.",
				NotificationType.Info,
				referenceId: returnRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

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
						await _notificationService.SendToUserAsync(
							   requestForGhn.CustomerId,
							   "Yêu cầu trả hàng đã được chấp thuận",
							   $"Yêu cầu trả đơn #{requestForGhn.OrderId} của bạn đã được chấp thuận và đang tạo vận đơn hoàn trả.",
							   NotificationType.Success,
							   referenceId: requestForGhn.Id,
							   referenceType: NotifiReferecneType.OrderReturnRequest);

						return BaseResponse<string>.Ok(
							"Return request approved locally, BUT failed to create GHN return shipping order. Please check GHN configuration and retry manually.");
					}
				}
				catch (Exception ex)
				{
					await _notificationService.SendToUserAsync(
						   requestForGhn.CustomerId,
						   "Yêu cầu trả hàng đã được chấp thuận",
						   $"Yêu cầu trả đơn #{requestForGhn.OrderId} của bạn đã được chấp thuận và đang xử lý vận chuyển hoàn trả.",
						   NotificationType.Success,
						   referenceId: requestForGhn.Id,
						   referenceType: NotifiReferecneType.OrderReturnRequest);

					return BaseResponse<string>.Ok(
						$"Return request approved locally, BUT GHN API threw an error: {ex.Message}. Please retry sync manually.");
				}

				await _notificationService.SendToUserAsync(
					requestForGhn.CustomerId,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{requestForGhn.OrderId} của bạn đã được chấp thuận.",
					NotificationType.Success,
					referenceId: requestForGhn.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Return request approved for shipment and GHN order created successfully.");
			}

			if (request.IsApproved)
			{
				var approvedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					  ?? throw AppException.NotFound("Return request not found.");

				await _notificationService.SendToUserAsync(
					approvedRequest.CustomerId,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{approvedRequest.OrderId} của bạn đã được chấp thuận.",
					NotificationType.Success,
					referenceId: approvedRequest.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Return request approved and moved to refund processing.");
			}

			if (request.IsRequestMoreInfo)
			{
				var userRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					  ?? throw AppException.NotFound("Return request not found.");

				await _notificationService.SendToUserAsync(
					userRequest.CustomerId,
					"Cần bổ sung bằng chứng",
					$"Vui lòng cập nhật thêm bằng chứng cho đơn #{userRequest.OrderId}.",
					NotificationType.Warning,
					referenceId: userRequest.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Return request requires more information from customer.");
			}

			var rejectedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
				   ?? throw AppException.NotFound("Return request not found.");

			await _notificationService.SendToUserAsync(
				rejectedRequest.CustomerId,
				"Yêu cầu trả hàng đã bị từ chối",
				$"Yêu cầu trả đơn #{rejectedRequest.OrderId} của bạn đã bị từ chối.",
				NotificationType.Warning,
				referenceId: rejectedRequest.Id,
				referenceType: NotifiReferecneType.OrderReturnRequest);

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

				var order = returnRequest.Order;
               var refundableOrderAmount = await GetRefundableOrderAmountAsync(order.Id, order.TotalAmount, IsFullOrderReturn(returnRequest));
				var isFullyRefunded = request.ApprovedRefundAmount >= refundableOrderAmount;

				if (isFullyRefunded)
				{
					if (order.Status == OrderStatus.Returning)
					{
						order.SetStatus(OrderStatus.Returned);
					}
				}
				else
				{
					if (order.Status == OrderStatus.Returning)
					{
						order.SetStatus(OrderStatus.Partial_Returned);
					}
				}

				if (returnRequest.IsRestocked)
				{
					var stockAdjustment = StockAdjustment.Create(
						inspectedById,
						DateTime.UtcNow,
						StockAdjustmentReason.Return,
						$"Auto restock from return request {returnRequest.Id}");

					var orderDetailsById = returnRequest.Order.OrderDetails
						  .ToDictionary(d => d.Id);

					if (returnRequest.ReturnDetails == null || returnRequest.ReturnDetails.Count == 0)
						throw AppException.Internal("Return details are missing. Cannot process restocking.");

					foreach (var returnDetail in returnRequest.ReturnDetails)
					{
						var orderDetail = orderDetailsById[returnDetail.OrderDetailId];

						// 💥 GIẢI MÃ BATCH ID TỪ SNAPSHOT CỦA CHÍNH CÁI CHAI KHÁCH TRẢ
						Guid batchId;
						try
						{
							using var doc = JsonDocument.Parse(orderDetail.Snapshot);
							batchId = doc.RootElement.GetProperty("BatchId").GetGuid();
						}
						catch
						{
							throw AppException.Internal($"Failed to extract BatchId from OrderDetail {orderDetail.Id} snapshot.");
						}

						var quantityToRestock = returnDetail.RequestedQuantity;
						if (quantityToRestock <= 0) continue;

                      _ = await _unitOfWork.Batches.GetByIdAsync(batchId)
							?? throw AppException.NotFound($"Batch {batchId} found in snapshot does not exist in database.");

						// Ghi log nhập kho (Stock Adjustment)
						var detailPayload = new StockAdjustmentDetailPayload
						{
							ProductVariantId = orderDetail.VariantId,
							BatchId = batchId,
							AdjustmentQuantity = quantityToRestock,
                            Note = $"Return request {returnRequest.Id}: moved to defective/holding inventory"
						};

						stockAdjustment.AddApprovedDetail(detailPayload, approvedQuantity: quantityToRestock);
					}

					stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					stockAdjustment.Complete(inspectedById);
					await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
				}

				_unitOfWork.Orders.Update(order);
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
          var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
				?? throw AppException.NotFound("Return request not found.");
            var refundableOrderAmount = await GetRefundableOrderAmountAsync(returnRequest.OrderId, returnRequest.Order.TotalAmount, IsFullOrderReturn(returnRequest));

			if (returnRequest.Status != ReturnRequestStatus.ReadyForRefund)
				throw AppException.BadRequest("Return request is not ready for refund.");

			if (!returnRequest.ApprovedRefundAmount.HasValue || returnRequest.ApprovedRefundAmount.Value <= 0)
				throw AppException.BadRequest("Approved refund amount must be greater than 0.");

			bool isRefundSuccess = false;
			string refundMessage = "";
			string? refundTransactionNo = null;
			Guid originalPaymentId;
			decimal originalPaymentAmount = 0;

			var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
				p => p.OrderId == returnRequest.OrderId && p.TransactionStatus == TransactionStatus.Success))
				.OrderByDescending(p => p.CreatedAt).ToList();

			var httpContext = _httpContextAccessor.HttpContext
				?? throw AppException.Internal("HttpContext not available.");

			switch (request.RefundMethod)
			{
				case PaymentMethod.VnPay:

					var vnPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
						?? throw AppException.NotFound($"No successful {request.RefundMethod} payment found for this order.");

					originalPaymentId = vnPayment.Id;
					originalPaymentAmount = vnPayment.Amount;

					var vnPayRefundResponse = await _vnPayService.RefundAsync(httpContext, new VnPayRefundRequest
					{
						OrderId = returnRequest.OrderId,
						Amount = returnRequest.ApprovedRefundAmount.Value,
						PaymentId = vnPayment.Id,
						TransactionNo = vnPayment.GatewayTransactionNo,
						TransactionType = returnRequest.ApprovedRefundAmount.Value == vnPayment.Amount ? "02" : "03",
						CreateBy = financeAdminId.ToString(),
						OrderInfo = $"Refund for return request {requestId}",
						TransactionDate = vnPayment.CreatedAt.ToString("yyyyMMddHHmmss")
					});

					isRefundSuccess = vnPayRefundResponse.IsSuccess;
					refundMessage = vnPayRefundResponse.Message;
					refundTransactionNo = vnPayRefundResponse.TransactionNo;
					break;

				case PaymentMethod.Momo:

					var momoPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
						?? throw AppException.NotFound($"No successful {request.RefundMethod} payment found for this order.");

					originalPaymentId = momoPayment.Id;
					originalPaymentAmount = momoPayment.Amount;

					var momoRefundResponse = await _momoService.RefundAsync(httpContext, new MomoRefundRequest
					{
						OrderId = returnRequest.OrderId,
						OrderCode = returnRequest.Order.Code,
						Amount = returnRequest.ApprovedRefundAmount.Value,
						PaymentId = momoPayment.Id,
						TransactionNo = momoPayment.GatewayTransactionNo,
						Description = $"Refund for return request {requestId}"
					});

					isRefundSuccess = momoRefundResponse.IsSuccess;
					refundMessage = momoRefundResponse.Message;
					refundTransactionNo = momoRefundResponse.TransactionNo;
					break;

				case PaymentMethod.ExternalBankTransfer:
				case PaymentMethod.CashInStore:

					if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
						throw AppException.BadRequest("Manual transaction reference (Bank transfer code) is required for manual refunds.");

					var primaryPayment = successfulOnlinePayments.FirstOrDefault()
						?? throw AppException.NotFound("No successful payment found for this order to reference for manual refund.");

					originalPaymentId = primaryPayment.Id;
					// Lấy amount từ payment, nếu payment = 0 (trường hợp COD không lưu amount) thì lấy tổng đơn hàng
					originalPaymentAmount = primaryPayment.Amount > 0 ? primaryPayment.Amount : returnRequest.Order.TotalAmount;

					isRefundSuccess = true;
					refundMessage = request.Note ?? "Manual refund recorded by Finance Admin";
					refundTransactionNo = request.ManualTransactionReference.Trim();
					break;

				default:
					throw AppException.BadRequest($"Refund is not supported for payment method {request.RefundMethod}.");
			}

			if (!isRefundSuccess)
			{
				var failedRefund = PaymentTransaction.CreateRefund(
					orderId: returnRequest.OrderId,
					originalPaymentId: originalPaymentId,
					method: request.RefundMethod,
					refundAmount: returnRequest.ApprovedRefundAmount.Value
				);
				failedRefund.MarkFailed(refundMessage, refundTransactionNo);
				await _unitOfWork.Payments.AddAsync(failedRefund);

				throw AppException.BadRequest($"Refund failed via {request.RefundMethod}: {refundMessage}");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
             var freshReturnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					?? throw AppException.NotFound("Return request not found during transaction.");

				if (freshReturnRequest.Status != ReturnRequestStatus.ReadyForRefund)
					throw AppException.BadRequest("Return request status changed and is no longer refundable.");

				var order = freshReturnRequest.Order;
				var approvedRefundAmount = freshReturnRequest.ApprovedRefundAmount!.Value;
				var freshRefundableAmount = await GetRefundableOrderAmountAsync(order.Id, order.TotalAmount, IsFullOrderReturn(freshReturnRequest));
				var refundableBaseline = Math.Min(freshRefundableAmount, originalPaymentAmount);
				var isFullyRefunded = approvedRefundAmount >= refundableBaseline;

				if (isFullyRefunded)
				{
					order.MarkRefunded();

					if (freshReturnRequest.IsRefundOnly && order.Status != OrderStatus.Returned)
					{
						if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Returning)
						{
							order.SetStatus(OrderStatus.Returned);
						}
					}
				}
				else
				{
					order.MarkPartiallyRefunded();

					if (freshReturnRequest.IsRefundOnly)
					{
						if (order.Status == OrderStatus.Delivered)
						{
							order.SetStatus(OrderStatus.Returning);
						}

						if (order.Status == OrderStatus.Returning)
						{
							order.SetStatus(OrderStatus.Partial_Returned);
						}
					}
				}

				var successRefund = PaymentTransaction.CreateRefund(
					orderId: order.Id,
					originalPaymentId: originalPaymentId,
					method: request.RefundMethod,
					refundAmount: approvedRefundAmount
				);

				successRefund.MarkSuccess(refundTransactionNo);
				freshReturnRequest.MarkRefunded(refundTransactionNo);

				_unitOfWork.Orders.Update(order);
				await _unitOfWork.Payments.AddAsync(successRefund);

				return BaseResponse<string>.Ok("Refund processed and return request completed.");
			});
		}

       private static bool IsFullOrderReturn(Dictionary<Guid, OrderDetail> orderDetailsById, IEnumerable<ReturnItemDto> returnItems)
		{
			var requestedByOrderDetailId = returnItems
				.GroupBy(x => x.OrderDetailId)
				.ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

			return orderDetailsById.All(kvp =>
				requestedByOrderDetailId.TryGetValue(kvp.Key, out var qty)
				&& qty >= kvp.Value.Quantity);
		}

		private static bool IsFullOrderReturn(OrderReturnRequest returnRequest)
		{
			var orderDetailsById = returnRequest.Order.OrderDetails.ToDictionary(x => x.Id, x => x.Quantity);
			var requestedByOrderDetailId = returnRequest.ReturnDetails
				.GroupBy(x => x.OrderDetailId)
				.ToDictionary(g => g.Key, g => g.Sum(x => x.RequestedQuantity));

			return orderDetailsById.All(kvp =>
				requestedByOrderDetailId.TryGetValue(kvp.Key, out var qty)
				&& qty >= kvp.Value);
		}

		private async Task<decimal> GetRefundableOrderAmountAsync(Guid orderId, decimal orderTotalAmount, bool includeShippingFee)
		{
           if (includeShippingFee)
				return Math.Max(0m, orderTotalAmount);

			var forwardShipping = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(orderId);
			var shippingFee = forwardShipping?.ShippingFee ?? 0m;
			return Math.Max(0m, orderTotalAmount - shippingFee);
		}
	}
}
