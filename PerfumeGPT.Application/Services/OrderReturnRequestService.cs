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
				"Lấy danh sách yêu cầu trả hàng thành công.");
		}

		public async Task<BaseResponse<PagedResult<OrderReturnRequestResponse>>> GetPagedUserReturnRequestsAsync(Guid userId, GetPagedUserReturnRequestsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.OrderReturnRequests.GetPagedUserResponsesAsync(userId, request);
			items = MaskRefundAccountNumbers(items);

			return BaseResponse<PagedResult<OrderReturnRequestResponse>>.Ok(
				new PagedResult<OrderReturnRequestResponse>(items, request.PageNumber, request.PageSize, totalCount),
				"Lấy danh sách yêu cầu trả hàng thành công.");
		}

		public async Task<BaseResponse<OrderReturnRequestResponse>> GetReturnRequestByIdAsync(Guid requestId, Guid requesterId, bool isPrivilegedUser)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetResponseByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			if (!isPrivilegedUser && returnRequest.CustomerId != requesterId)
				throw AppException.Forbidden("Bạn không có quyền xem yêu cầu trả hàng này.");

			returnRequest = MaskRefundAccountNumbers(returnRequest);

			return BaseResponse<OrderReturnRequestResponse>.Ok(returnRequest, "Lấy thông tin yêu cầu trả hàng thành công.");
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
			// 1. Fast-fail: Tránh mở Transaction vô ích nếu dữ liệu không hợp lệ
			if (request.ReturnItems == null || request.ReturnItems.Count == 0)
				throw AppException.BadRequest("Danh sách sản phẩm trả về không được để trống.");

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(request.OrderId)
					 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (order.CustomerId != customerId)
					throw AppException.Forbidden("Bạn không có quyền tạo yêu cầu trả hàng cho đơn này.");

				if (order.Status != OrderStatus.Delivered)
					throw AppException.BadRequest("Chỉ đơn hàng đã giao mới có thể tạo yêu cầu trả hàng.");

				// 2. TỐI ƯU DB: Lấy tất cả Status bằng 1 câu query duy nhất (Tiết kiệm 1 round-trip)
				var existingStatuses = await _unitOfWork.OrderReturnRequests.GetStatusesByOrderIdAsync(order.Id);

				if (existingStatuses.Any(s => s != ReturnRequestStatus.Rejected && s != ReturnRequestStatus.Completed))
					throw AppException.Conflict("Đơn hàng này đã có yêu cầu trả hàng đang xử lý.");

				if (existingStatuses.Any(s => s == ReturnRequestStatus.Completed))
					throw AppException.Conflict("Đơn hàng này đã từng được trả trước đó. Vui lòng liên hệ hỗ trợ để được xử lý thêm.");

				var orderDetailsById = order.OrderDetails.ToDictionary(x => x.Id);

				// 3. TỐI ƯU VÒNG LẶP: Cấp phát bộ nhớ sẵn cho List và tính toán trong 1 lần duyệt
				var payloadDetails = new List<OrderReturnRequest.ReturnRequestDetailPayload>(request.ReturnItems.Count);
				decimal requestedRefundAmount = 0m;

				foreach (var item in request.ReturnItems)
				{
					if (!orderDetailsById.TryGetValue(item.OrderDetailId, out var orderDetail))
						throw AppException.BadRequest($"Chi tiết đơn hàng {item.OrderDetailId} không thuộc đơn này.");

					if (item.Quantity > orderDetail.Quantity)
						throw AppException.BadRequest($"Số lượng trả cho chi tiết đơn hàng {item.OrderDetailId} không được vượt quá số lượng đã mua.");

					payloadDetails.Add(new OrderReturnRequest.ReturnRequestDetailPayload
					{
						OrderDetailId = item.OrderDetailId,
						RequestedQuantity = item.Quantity,
						OrderedQuantity = orderDetail.Quantity
					});

					// Tính tổng tiền ngay tại đây
					requestedRefundAmount += orderDetail.RefundableUnitPrice * item.Quantity;
				}

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

				return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Tạo yêu cầu trả hàng thành công.");
			});

			if (!Guid.TryParse(response.Payload, out var createdRequestId))
				throw AppException.Internal("Không thể phân tích ID yêu cầu trả hàng.");

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
				return BaseResponse<string>.Ok(createdRequestId.ToString(), "Tạo yêu cầu trả hàng thành công.");

			var message = conversionResult.HasError
			   ? $"Tạo yêu cầu trả hàng thành công nhưng có {conversionResult.FailedItems.Count} ảnh minh chứng tải lên thất bại."
				: "Tạo yêu cầu trả hàng thành công.";

			return BaseResponse<string>.Ok(createdRequestId.ToString(), message);
		}

		public async Task<BaseResponse<string>> UpdateReturnRequestAsync(Guid customerId, Guid requestId, UpdateReturnRequestDto request)
		{
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				  if (returnRequest.CustomerId != customerId)
					  throw AppException.Forbidden("Bạn không có quyền cập nhật yêu cầu trả hàng này.");

				  if (request.RemoveMediaIds != null && request.RemoveMediaIds.Count > 0)
				  {
					  var existingMediaIds = returnRequest.ProofImages.Select(x => x.Id).ToHashSet();
					  var invalidMediaId = request.RemoveMediaIds.FirstOrDefault(id => !existingMediaIds.Contains(id));

					  if (invalidMediaId != Guid.Empty)
						  throw AppException.BadRequest($"Media {invalidMediaId} không thuộc yêu cầu trả hàng này.");
				  }

				  returnRequest.UpdateByCustomer(
						 customerId,
						 request.CustomerNote,
						 request.RefundBankName,
						 request.RefundAccountNumber,
						 request.RefundAccountName);
				  _unitOfWork.OrderReturnRequests.Update(returnRequest);

				  return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Yêu cầu trả hàng đã được cập nhật và gửi lại để xét duyệt.");
			  });

			if (request.RemoveMediaIds != null && request.RemoveMediaIds.Count > 0)
			{
				await _mediaBulkActionHelper.DeleteMultipleMediaAsync(request.RemoveMediaIds);
			}

			if (request.TemporaryMediaIds == null || request.TemporaryMediaIds.Count == 0)
				return response;

			if (!Guid.TryParse(response.Payload, out var returnRequestId))
				throw AppException.Internal("Không thể phân tích ID yêu cầu trả hàng.");

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
			  ? $"Cập nhật yêu cầu trả hàng thành công nhưng có {conversionResult.FailedItems.Count} tệp ảnh/video minh chứng tải lên thất bại."
				: "Yêu cầu trả hàng đã được cập nhật và gửi lại để xét duyệt.";

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
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				returnRequest.CancelByCustomer(customerId);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Đã hủy yêu cầu trả hàng.");
			});
		}

		public async Task<BaseResponse<string>> ProcessInitialRequestAsync(Guid processedById, Guid requestId, ProcessInitialReturnDto request)
		{
			OrderReturnRequest? requestForGhn = null;
			ContactAddress? contactInfoForGhn = null;

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithPickAddressAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				returnRequest.Process(processedById, request.IsApproved, request.IsRequestMoreInfo, request.StaffNote);

				if (request.IsApproved && !returnRequest.IsRefundOnly)
				{
					var contactInfo = returnRequest.PickupAddress ?? throw AppException.Internal("Không tìm thấy địa chỉ lấy hàng cho yêu cầu trả hàng đã duyệt.");
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
							   "Yêu cầu trả hàng đã được duyệt cục bộ, NHƯNG tạo đơn hoàn trả GHN thất bại. Vui lòng kiểm tra cấu hình GHN và thử đồng bộ lại thủ công.");
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
						   $"Yêu cầu trả hàng đã được duyệt cục bộ, NHƯNG GHN API trả lỗi: {ex.Message}. Vui lòng thử đồng bộ lại thủ công.");
				}

				await _notificationService.SendToUserAsync(
					requestForGhn.CustomerId,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{requestForGhn.OrderId} của bạn đã được chấp thuận.",
					NotificationType.Success,
					referenceId: requestForGhn.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Đã duyệt yêu cầu trả hàng để vận chuyển và tạo đơn GHN thành công.");
			}

			if (request.IsApproved)
			{
				var approvedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				await _notificationService.SendToUserAsync(
					approvedRequest.CustomerId,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{approvedRequest.OrderId} của bạn đã được chấp thuận.",
					NotificationType.Success,
					referenceId: approvedRequest.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Yêu cầu trả hàng đã được duyệt và chuyển sang bước hoàn tiền.");
			}

			if (request.IsRequestMoreInfo)
			{
				var userRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				await _notificationService.SendToUserAsync(
					userRequest.CustomerId,
					"Cần bổ sung bằng chứng",
					$"Vui lòng cập nhật thêm bằng chứng cho đơn #{userRequest.OrderId}.",
					NotificationType.Warning,
					referenceId: userRequest.Id,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				return BaseResponse<string>.Ok("Yêu cầu trả hàng cần khách hàng bổ sung thêm thông tin.");
			}

			var rejectedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			await _notificationService.SendToUserAsync(
				rejectedRequest.CustomerId,
				"Yêu cầu trả hàng đã bị từ chối",
				$"Yêu cầu trả đơn #{rejectedRequest.OrderId} của bạn đã bị từ chối.",
				NotificationType.Warning,
				referenceId: rejectedRequest.Id,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			return BaseResponse<string>.Ok("Yêu cầu trả hàng đã bị từ chối.");
		}

		public async Task<BaseResponse<string>> StartInspectionAsync(Guid inspectedById, Guid requestId, StartInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				bool isArrived = true;

				if (returnRequest.ReturnShipping != null)
				{
					isArrived = returnRequest.ReturnShipping.Status == ShippingStatus.Delivered;
				}

				if (!isArrived)
				{
					throw AppException.BadRequest("Kiện hàng trả chưa được giao đến cửa hàng. Không thể bắt đầu kiểm định.");
				}

				returnRequest.StartInspection(inspectedById, request.InspectionNote);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Đã bắt đầu kiểm định.");
			});
		}

		public async Task<BaseResponse<string>> RecordInspectionResultAsync(Guid inspectedById, Guid requestId, RecordInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				if (returnRequest.InspectedById != inspectedById)
					throw AppException.Forbidden("Chỉ người kiểm định được phân công mới có thể ghi nhận kết quả kiểm định.");

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
						throw AppException.Internal("Thiếu chi tiết trả hàng. Không thể xử lý nhập trả kho.");

					foreach (var returnDetail in returnRequest.ReturnDetails)
					{
						var orderDetail = orderDetailsById[returnDetail.OrderDetailId];

						// GIẢI MÃ BATCH ID TỪ SNAPSHOT CỦA CHÍNH CÁI CHAI KHÁCH TRẢ
						Guid batchId;
						try
						{
							using var doc = JsonDocument.Parse(orderDetail.Snapshot);
							batchId = doc.RootElement.GetProperty("BatchId").GetGuid();
						}
						catch
						{
							throw AppException.Internal($"Không thể trích xuất BatchId từ snapshot của OrderDetail {orderDetail.Id}.");
						}

						var quantityToRestock = returnDetail.RequestedQuantity;
						if (quantityToRestock <= 0) continue;

						_ = await _unitOfWork.Batches.GetByIdAsync(batchId)
						   ?? throw AppException.NotFound($"Lô {batchId} trong snapshot không tồn tại trong cơ sở dữ liệu.");

						// Ghi log nhập kho (Stock Adjustment)
						var detailPayload = new StockAdjustmentDetailPayload
						{
							ProductVariantId = orderDetail.VariantId,
							BatchId = batchId,
							AdjustmentQuantity = quantityToRestock,
							Note = $"Yêu cầu trả hàng {returnRequest.Id}: chuyển về tồn kho hàng lỗi/chờ xử lý"
						};

						stockAdjustment.AddApprovedDetail(detailPayload, approvedQuantity: quantityToRestock);
					}

					stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					stockAdjustment.Complete(inspectedById);
					await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
				}

				_unitOfWork.Orders.Update(order);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Đã ghi nhận kết quả kiểm định.");
			});
		}

		public async Task<BaseResponse<string>> RejectAfterInspectionAsync(Guid inspectedById, Guid requestId, RejectInspectionDto request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				returnRequest.RejectAfterInspection(inspectedById, request.Note);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Yêu cầu trả hàng đã bị từ chối sau kiểm định.");
			});
		}

		public async Task<BaseResponse<string>> ProcessRefundAsync(Guid financeAdminId, Guid requestId, ProcessRefundRequest request)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			var refundableOrderAmount = await GetRefundableOrderAmountAsync(returnRequest.OrderId, returnRequest.Order.TotalAmount, IsFullOrderReturn(returnRequest));

			if (returnRequest.Status != ReturnRequestStatus.ReadyForRefund)
				throw AppException.BadRequest("Yêu cầu trả hàng chưa sẵn sàng để hoàn tiền.");

			// 1. Kiểm tra giá trị do Admin truyền vào thay vì giá trị cũ của Staff
			if (request.ApprovedRefundAmount <= 0)
				throw AppException.BadRequest("Số tiền hoàn được duyệt phải lớn hơn 0.");

			if (request.ApprovedRefundAmount > refundableOrderAmount)
				throw AppException.BadRequest($"Số tiền hoàn vượt quá mức cho phép. Tối đa có thể hoàn: {refundableOrderAmount}");

			bool isRefundSuccess = false;
			string refundMessage = "";
			string? refundTransactionNo = null;
			Guid originalPaymentId;
			decimal originalPaymentAmount = 0;

			var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
				p => p.OrderId == returnRequest.OrderId && p.TransactionStatus == TransactionStatus.Success))
				.OrderByDescending(p => p.CreatedAt).ToList();

			var httpContext = _httpContextAccessor.HttpContext
			   ?? throw AppException.Internal("HttpContext hiện không khả dụng.");

			switch (request.RefundMethod)
			{
				case PaymentMethod.VnPay:
					var vnPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
					  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {request.RefundMethod} thành công cho đơn hàng này.");

					originalPaymentId = vnPayment.Id;
					originalPaymentAmount = vnPayment.Amount;

					var vnPayRefundResponse = await _vnPayService.RefundAsync(httpContext, new VnPayRefundRequest
					{
						OrderId = returnRequest.OrderId,
						Amount = request.ApprovedRefundAmount, // Sử dụng giá trị ghi đè
						PaymentId = vnPayment.Id,
						TransactionNo = vnPayment.GatewayTransactionNo,
						TransactionType = request.ApprovedRefundAmount == vnPayment.Amount ? "02" : "03",
						CreateBy = financeAdminId.ToString(),
						OrderInfo = $"Hoàn tiền cho yêu cầu trả hàng {requestId}",
						TransactionDate = vnPayment.CreatedAt.ToString("yyyyMMddHHmmss")
					});

					isRefundSuccess = vnPayRefundResponse.IsSuccess;
					refundMessage = vnPayRefundResponse.Message;
					refundTransactionNo = vnPayRefundResponse.TransactionNo;
					break;

				case PaymentMethod.Momo:
					var momoPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
					  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {request.RefundMethod} thành công cho đơn hàng này.");

					originalPaymentId = momoPayment.Id;
					originalPaymentAmount = momoPayment.Amount;

					var momoRefundResponse = await _momoService.RefundAsync(httpContext, new MomoRefundRequest
					{
						OrderId = returnRequest.OrderId,
						OrderCode = returnRequest.Order.Code,
						Amount = request.ApprovedRefundAmount, // Sử dụng giá trị ghi đè
						PaymentId = momoPayment.Id,
						TransactionNo = momoPayment.GatewayTransactionNo,
						Description = $"Hoàn tiền cho yêu cầu trả hàng {requestId}"
					});

					isRefundSuccess = momoRefundResponse.IsSuccess;
					refundMessage = momoRefundResponse.Message;
					refundTransactionNo = momoRefundResponse.TransactionNo;
					break;

				case PaymentMethod.ExternalBankTransfer:
				case PaymentMethod.CashInStore:
					if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
						throw AppException.BadRequest("Bắt buộc nhập mã tham chiếu giao dịch thủ công (mã chuyển khoản) cho hoàn tiền thủ công.");

					var primaryPayment = successfulOnlinePayments.FirstOrDefault()
					   ?? throw AppException.NotFound("Không tìm thấy giao dịch thanh toán thành công của đơn hàng để đối chiếu hoàn tiền thủ công.");

					originalPaymentId = primaryPayment.Id;
					originalPaymentAmount = primaryPayment.Amount > 0 ? primaryPayment.Amount : returnRequest.Order.TotalAmount;

					isRefundSuccess = true;
					refundMessage = request.Note ?? "Đã ghi nhận hoàn tiền thủ công bởi quản trị tài chính";
					refundTransactionNo = request.ManualTransactionReference.Trim();
					break;

				default:
					throw AppException.BadRequest($"Không hỗ trợ hoàn tiền cho phương thức thanh toán {request.RefundMethod}.");
			}

			if (!isRefundSuccess)
			{
				var failedRefund = PaymentTransaction.CreateRefund(
					orderId: returnRequest.OrderId,
					originalPaymentId: originalPaymentId,
					method: request.RefundMethod,
					refundAmount: request.ApprovedRefundAmount // Ghi log giá trị ghi đè
				);
				failedRefund.MarkFailed(refundMessage, refundTransactionNo);
				await _unitOfWork.Payments.AddAsync(failedRefund);

				throw AppException.BadRequest($"Hoàn tiền qua {request.RefundMethod} thất bại: {refundMessage}");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var freshReturnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderDetailsAsync(requestId)
					 ?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng trong phiên giao dịch.");

				if (freshReturnRequest.Status != ReturnRequestStatus.ReadyForRefund)
					throw AppException.BadRequest("Trạng thái yêu cầu trả hàng đã thay đổi và không còn có thể hoàn tiền.");

				var order = freshReturnRequest.Order;

				// Entity sẽ tự động check logic, ghi đè số tiền và nối chuỗi StaffNote
				freshReturnRequest.OverrideRefundAmount(request.ApprovedRefundAmount, request.Note);

				// Lấy số tiền sau khi đã được Domain xác nhận
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

				return BaseResponse<string>.Ok("Đã xử lý hoàn tiền và hoàn tất yêu cầu trả hàng.");
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
