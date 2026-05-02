using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System;
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
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<OrderReturnRequestService> _logger;

		public OrderReturnRequestService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor,
			MediaBulkActionHelper mediaBulkActionHelper,
			IOrderShippingHelper orderShippingHelper,
			IContactAddressService recipientService,
			IBackgroundJobService backgroundJobService,
			ILogger<OrderReturnRequestService> logger)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
			_mediaBulkActionHelper = mediaBulkActionHelper;
			_orderShippingHelper = orderShippingHelper;
			_recipientService = recipientService;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
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

		private void TryNotifyReturnRequestCustomer(
			Guid? customerId,
			Guid returnRequestId,
			string title,
			string message,
			NotificationType type = NotificationType.Info)
		{
			if (!customerId.HasValue)
				return;

			_backgroundJobService.EnqueueCustomerNotificationWithFcm(
				_logger,
				customerId.Value,
				title,
				message,
				type,
				returnRequestId,
				NotifiReferecneType.OrderReturnRequest);
		}

		public async Task<BaseResponse<string>> CreateReturnRequestAsync(Guid customerId, CreateReturnRequestDto request)
		{
			if (request.ReturnItems == null || request.ReturnItems.Count == 0)
				throw AppException.BadRequest("Danh sách sản phẩm trả về không được để trống.");

			return await CreateReturnRequestCoreAsync(
				request,
				returnRequestCustomerId: customerId,
				contactAddressCustomerId: customerId,
				order =>
				{
					if (!string.Equals(order.Code, request.OrderCode, StringComparison.Ordinal))
						throw AppException.BadRequest("Mã đơn hàng không khớp với đơn đã chọn.");
					if (order.CustomerId != customerId)
						throw AppException.Forbidden("Bạn không có quyền tạo yêu cầu trả hàng cho đơn này.");
				},
				roleNotifyDetail: $"Khách hàng đã yêu cầu trả đơn #{request.OrderCode}.");
		}

		public async Task<BaseResponse<string>> CreateGuestReturnRequestByStaffAsync(Guid staffId, CreateReturnRequestDto request)
		{
			if (request.ReturnItems == null || request.ReturnItems.Count == 0)
				throw AppException.BadRequest("Danh sách sản phẩm trả về không được để trống.");
			if (request.SavedAddressId.HasValue)
				throw AppException.BadRequest("Đơn khách vãng lai không dùng được địa chỉ đã lưu; vui lòng nhập đầy đủ thông tin địa chỉ lấy hàng theo khách cung cấp.");
			if (request.Recipient == null)
				throw AppException.BadRequest("Bắt buộc nhập thông tin người liên hệ và địa chỉ lấy hàng theo khách cung cấp.");

			var response = await CreateReturnRequestCoreAsync(
				request,
				returnRequestCustomerId: null,
				contactAddressCustomerId: null,
				order =>
				{
					if (!string.Equals(order.Code, request.OrderCode, StringComparison.Ordinal))
						throw AppException.BadRequest("Mã đơn hàng không khớp với đơn đã chọn.");
					if (order.CustomerId.HasValue)
						throw AppException.BadRequest("Chỉ tạo được yêu cầu trả hộ cho đơn không gắn tài khoản (khách vãng lai). Đơn này đã gắn tài khoản; khách vui lòng tạo yêu cầu qua ứng dụng.");
				},
				roleNotifyDetail: $"Nhân viên đã tạo yêu cầu trả hộ cho đơn khách vãng lai #{request.OrderCode}.");

			_logger.LogInformation("Staff {StaffId} created guest return request for order {OrderId}", staffId, request.OrderId);
			return response;
		}

		private async Task<BaseResponse<string>> CreateReturnRequestCoreAsync(
			CreateReturnRequestDto request,
			Guid? returnRequestCustomerId,
			Guid? contactAddressCustomerId,
			Action<Order> validateOrder,
			string roleNotifyDetail)
		{
			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForReturnRequestAsync(request.OrderId)
					 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				validateOrder(order);

				if (order.Status != OrderStatus.Delivered)
					throw AppException.BadRequest("Chỉ đơn hàng đã giao mới có thể tạo yêu cầu trả hàng.");

				var existingStatuses = await _unitOfWork.OrderReturnRequests.GetStatusesByOrderIdAsync(order.Id);

				if (existingStatuses.Any(s => s != ReturnRequestStatus.Rejected && s != ReturnRequestStatus.Completed))
					throw AppException.Conflict("Đơn hàng này đã có yêu cầu trả hàng đang xử lý.");

				if (existingStatuses.Any(s => s == ReturnRequestStatus.Completed))
					throw AppException.Conflict("Đơn hàng này đã từng được trả trước đó. Vui lòng liên hệ hỗ trợ để được xử lý thêm.");

				var orderDetailsById = order.OrderDetails.ToDictionary(x => x.Id);

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

				var pickupAddress = await _recipientService.CreateContactAddressAsync(request.Recipient, request.SavedAddressId, contactAddressCustomerId);

				var returnRequest = OrderReturnRequest.Create(request.OrderId, returnRequestCustomerId, requestPayload);
				returnRequest.AttachPickupAddress(pickupAddress.Id);
				returnRequest.PickupAddress = pickupAddress;

				await _unitOfWork.OrderReturnRequests.AddAsync(returnRequest);

				return BaseResponse<string>.Ok(returnRequest.Id.ToString(), "Tạo yêu cầu trả hàng thành công.");
			});

			if (!Guid.TryParse(response.Payload, out var createdRequestId))
				throw AppException.Internal("Không thể phân tích ID yêu cầu trả hàng.");

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.admin,
				"Yêu cầu trả hàng mới",
				roleNotifyDetail,
				NotificationType.Warning,
				referenceId: createdRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.staff,
				"Yêu cầu trả hàng mới",
				roleNotifyDetail,
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
				_backgroundJobService.EnqueueRoleNotification(
					_logger,
					UserRole.admin,
					"Khách đã bổ sung bằng chứng trả hàng",
					$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{requestId}.",
					NotificationType.Info,
					referenceId: requestId,
					referenceType: NotifiReferecneType.OrderReturnRequest);

				_backgroundJobService.EnqueueRoleNotification(
					_logger,
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

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
				UserRole.admin,
				"Khách đã bổ sung bằng chứng trả hàng",
				$"Khách hàng đã bổ sung bằng chứng cho yêu cầu trả #{returnRequestId}.",
				NotificationType.Info,
				referenceId: returnRequestId,
				referenceType: NotifiReferecneType.OrderReturnRequest);

			_backgroundJobService.EnqueueRoleNotification(
				_logger,
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
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			returnRequest.CancelByCustomer(customerId);
			_unitOfWork.OrderReturnRequests.Update(returnRequest);
			await _unitOfWork.SaveChangesAsync();
			return BaseResponse<string>.Ok("Đã hủy yêu cầu trả hàng.");
		}

		public async Task<BaseResponse<string>> ProcessInitialRequestAsync(Guid processedById, Guid requestId, ProcessInitialReturnDto request)
		{
			OrderReturnRequest? requestForGhn = null;
			ContactAddress? contactInfoForGhn = null;
			DateTime? estimatedDeliveryDate = null;

			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdToProcessInitAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			var orderCode = returnRequest.Order.Code;

			if (request.IsApproved && !returnRequest.IsRefundOnly)
			{
				var pickupAddress = returnRequest.PickupAddress
					?? throw AppException.Internal("Không tìm thấy địa chỉ lấy hàng cho yêu cầu trả hàng đã duyệt.");
				estimatedDeliveryDate = await _orderShippingHelper.GetLeadTimeAsync(pickupAddress.DistrictId, pickupAddress.WardCode);
			}

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				returnRequest.Process(processedById, request.IsApproved, request.IsRequestMoreInfo, request.StaffNote);

				if (request.IsApproved && !returnRequest.IsRefundOnly)
				{
					var contactInfo = returnRequest.PickupAddress ?? throw AppException.Internal("Không tìm thấy địa chỉ lấy hàng cho yêu cầu trả hàng đã duyệt.");
					var leadTime = estimatedDeliveryDate
						?? throw AppException.Internal("Không thể xác định thời gian dự kiến giao trả cho yêu cầu trả hàng.");

					var returnShipping = ShippingInfo.Create(CarrierName.GHN, ShippingType.Return, 0, leadTime);
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
						TryNotifyReturnRequestCustomer(
							requestForGhn.CustomerId,
							requestForGhn.Id,
							"Yêu cầu trả hàng đã được chấp thuận",
							$"Yêu cầu trả đơn #{orderCode} của bạn đã được chấp thuận và đang tạo vận đơn hoàn trả.",
							NotificationType.Success);

						return BaseResponse<string>.Ok(
							   "Yêu cầu trả hàng đã được duyệt cục bộ, NHƯNG tạo đơn hoàn trả GHN thất bại. Vui lòng kiểm tra cấu hình GHN và thử đồng bộ lại thủ công.");
					}
				}
				catch (Exception ex)
				{
					TryNotifyReturnRequestCustomer(
						requestForGhn.CustomerId,
						requestForGhn.Id,
						"Yêu cầu trả hàng đã được chấp thuận",
						$"Yêu cầu trả đơn #{orderCode} của bạn đã được chấp thuận và đang xử lý vận chuyển hoàn trả.",
						NotificationType.Success);

					return BaseResponse<string>.Ok(
						   $"Yêu cầu trả hàng đã được duyệt cục bộ, NHƯNG GHN API trả lỗi: {ex.Message}. Vui lòng thử đồng bộ lại thủ công.");
				}

				TryNotifyReturnRequestCustomer(
					requestForGhn.CustomerId,
					requestForGhn.Id,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{orderCode} của bạn đã được chấp thuận.",
					NotificationType.Success);

				return BaseResponse<string>.Ok("Đã duyệt yêu cầu trả hàng để vận chuyển và tạo đơn GHN thành công.");
			}

			if (request.IsApproved)
			{
				var approvedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				TryNotifyReturnRequestCustomer(
					approvedRequest.CustomerId,
					approvedRequest.Id,
					"Yêu cầu trả hàng đã được chấp thuận",
					$"Yêu cầu trả đơn #{orderCode} của bạn đã được chấp thuận.",
					NotificationType.Success);

				return BaseResponse<string>.Ok("Yêu cầu trả hàng đã được duyệt và chuyển sang bước hoàn tiền.");
			}

			if (request.IsRequestMoreInfo)
			{
				var userRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

				TryNotifyReturnRequestCustomer(
					userRequest.CustomerId,
					userRequest.Id,
					"Cần bổ sung bằng chứng",
					$"Vui lòng cập nhật thêm bằng chứng cho đơn #{orderCode}.",
					NotificationType.Warning);

				return BaseResponse<string>.Ok("Yêu cầu trả hàng cần khách hàng bổ sung thêm thông tin.");
			}

			var rejectedRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			TryNotifyReturnRequestCustomer(
				rejectedRequest.CustomerId,
				rejectedRequest.Id,
				"Yêu cầu trả hàng đã bị từ chối",
				$"Yêu cầu trả đơn #{orderCode} của bạn đã bị từ chối.",
				NotificationType.Warning);

			return BaseResponse<string>.Ok("Yêu cầu trả hàng đã bị từ chối.");
		}

		public async Task<BaseResponse<string>> StartInspectionAsync(Guid inspectedById, Guid requestId, StartInspectionDto request)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdWithOrderAsync(requestId)
				?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			bool isArrived = returnRequest.ReturnShipping?.Status == ShippingStatus.Delivered;

			if (!isArrived)
				throw AppException.BadRequest("Kiện hàng trả chưa được giao đến cửa hàng. Không thể bắt đầu kiểm định.");

			returnRequest.StartInspection(inspectedById, request.InspectionNote);
			_unitOfWork.OrderReturnRequests.Update(returnRequest);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok("Đã bắt đầu kiểm định.");
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
					if (order.Status == OrderStatus.Returning) order.SetStatus(OrderStatus.Returned);
				}
				else
				{
					if (order.Status == OrderStatus.Returning) order.SetStatus(OrderStatus.Partial_Returned);
				}

				if (returnRequest.IsRestocked)
				{
					var stockAdjustment = StockAdjustment.Create(
						inspectedById,
						DateTime.UtcNow,
						StockAdjustmentReason.Return,
						$"Hệ thống tự động nhập kho từ yêu cầu trả hàng {returnRequest.Id}");

					var orderDetailsById = returnRequest.Order.OrderDetails.ToDictionary(d => d.Id);

					if (returnRequest.ReturnDetails == null || returnRequest.ReturnDetails.Count == 0)
						throw AppException.Internal("Thiếu chi tiết trả hàng. Không thể xử lý nhập trả kho.");

					// ==============================================================
					// 1. QUÉT 1 VÒNG ĐỂ LẤY TOÀN BỘ BATCH ID VÀ VARIANT ID
					// ==============================================================
					var batchIds = new HashSet<Guid>();
					var variantIds = new HashSet<Guid>();

					foreach (var returnDetail in returnRequest.ReturnDetails)
					{
						var orderDetail = orderDetailsById[returnDetail.OrderDetailId];
						try
						{
							using var doc = JsonDocument.Parse(orderDetail.Snapshot);
							batchIds.Add(doc.RootElement.GetProperty("BatchId").GetGuid());
							variantIds.Add(orderDetail.VariantId);
						}
						catch
						{
							throw AppException.Internal($"Không thể trích xuất BatchId từ snapshot của OrderDetail {orderDetail.Id}.");
						}
					}

					// ==============================================================
					// 2. BULK READ: Kéo dữ liệu lên RAM (Bỏ asNoTracking vì cần Update)
					// ==============================================================
					var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id));
					var batchesById = batches.ToDictionary(b => b.Id);

					var stocks = await _unitOfWork.Stocks.GetAllAsync(s => variantIds.Contains(s.VariantId));
					var stocksByVariantId = stocks.ToDictionary(s => s.VariantId);

					// ==============================================================
					// 3. XỬ LÝ LOGIC TRÊN RAM (IN-MEMORY UPDATE)
					// ==============================================================
					foreach (var returnDetail in returnRequest.ReturnDetails)
					{
						var orderDetail = orderDetailsById[returnDetail.OrderDetailId];
						var quantityToRestock = returnDetail.RequestedQuantity;

						if (quantityToRestock <= 0) continue;

						using var doc = JsonDocument.Parse(orderDetail.Snapshot);
						var batchId = doc.RootElement.GetProperty("BatchId").GetGuid();

						if (!batchesById.TryGetValue(batchId, out var batch))
							throw AppException.NotFound($"Lô {batchId} trong snapshot không tồn tại trong cơ sở dữ liệu.");

						if (!stocksByVariantId.TryGetValue(orderDetail.VariantId, out var stock))
							throw AppException.NotFound($"Tồn kho cho biến thể {orderDetail.VariantId} không tồn tại.");

						// 3.1. Ghi log phiếu nhập kho (StockAdjustment)
						var detailPayload = new StockAdjustmentDetailPayload
						{
							ProductVariantId = orderDetail.VariantId,
							BatchId = batchId,
							AdjustmentQuantity = quantityToRestock,
							Note = $"Yêu cầu trả hàng {returnRequest.Id}: nhập lại kho hàng hóa đạt tiêu chuẩn"
						};
						stockAdjustment.AddApprovedDetail(detailPayload, approvedQuantity: quantityToRestock);

						// 3.2. CỘNG LẠI SỐ LƯỢNG VẬT LÝ VÀO LÔ HÀNG (Điều mà Cursor đã quên)
						batch.IncreaseQuantity(
							quantityToRestock,
							StockTransactionType.Adjustment,
							returnRequest.Id,
							inspectedById,
							$"Nhập lại tồn kho từ đơn trả hàng {returnRequest.OrderId}");

						// 3.3. CỘNG LẠI TỔNG TỒN KHO CHO BIẾN THỂ
						stock.Increase(quantityToRestock);
					}

					stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
					stockAdjustment.Complete(inspectedById);
					await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);

					// ==============================================================
					// 4. BULK WRITE: Lưu toàn bộ lô hàng và tồn kho xuống DB bằng 1 mẻ
					// ==============================================================
					_unitOfWork.Batches.UpdateRange([.. batches]);
					_unitOfWork.Stocks.UpdateRange([.. stocks]);
				}

				_unitOfWork.Orders.Update(order);
				_unitOfWork.OrderReturnRequests.Update(returnRequest);

				return BaseResponse<string>.Ok("Đã ghi nhận kết quả kiểm định.");
			});
		}

		public async Task<BaseResponse<string>> RejectAfterInspectionAsync(Guid inspectedById, Guid requestId, RejectInspectionDto request)
		{
			var returnRequest = await _unitOfWork.OrderReturnRequests.GetByIdAsync(requestId)
					?? throw AppException.NotFound("Không tìm thấy yêu cầu trả hàng.");

			returnRequest.RejectAfterInspection(inspectedById, request.Note);
			_unitOfWork.OrderReturnRequests.Update(returnRequest);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok("Yêu cầu trả hàng đã bị từ chối sau kiểm định.");
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
			PaymentTransaction? pendingRefund = null;

			var successfulOnlinePayments = (await _unitOfWork.Payments.GetAllAsync(
				p => p.OrderId == returnRequest.OrderId && p.TransactionStatus == TransactionStatus.Success))
				.OrderByDescending(p => p.CreatedAt).ToList();

			PaymentTransaction? originalPayment = null;
			switch (request.RefundMethod)
			{
				case PaymentMethod.VnPay:
					originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
					  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {request.RefundMethod} thành công cho đơn hàng này.");
					break;

				case PaymentMethod.Momo:
					originalPayment = successfulOnlinePayments.FirstOrDefault(p => p.Method == request.RefundMethod)
					  ?? throw AppException.NotFound($"Không tìm thấy giao dịch {request.RefundMethod} thành công cho đơn hàng này.");
					break;

				case PaymentMethod.ExternalBankTransfer:
				case PaymentMethod.CashInStore:
					if (string.IsNullOrWhiteSpace(request.ManualTransactionReference))
						throw AppException.BadRequest("Bắt buộc nhập mã tham chiếu giao dịch thủ công (mã chuyển khoản) cho hoàn tiền thủ công.");

					originalPayment = successfulOnlinePayments.FirstOrDefault()
					   ?? throw AppException.NotFound("Không tìm thấy giao dịch thanh toán thành công của đơn hàng để đối chiếu hoàn tiền thủ công.");
					break;

				default:
					throw AppException.BadRequest($"Không hỗ trợ hoàn tiền cho phương thức thanh toán {request.RefundMethod}.");
			}
			originalPaymentId = originalPayment!.Id;
			originalPaymentAmount = originalPayment.Amount > 0 ? originalPayment.Amount : returnRequest.Order.TotalAmount;

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				pendingRefund = PaymentTransaction.CreateRefund(
					orderId: returnRequest.OrderId,
					originalPaymentId: originalPaymentId,
					method: request.RefundMethod,
					refundAmount: request.ApprovedRefundAmount
				);
				await _unitOfWork.Payments.AddAsync(pendingRefund);
				return true;
			});

			var httpContext = _httpContextAccessor.HttpContext
			   ?? throw AppException.Internal("HttpContext hiện không khả dụng.");

			switch (request.RefundMethod)
			{
				case PaymentMethod.VnPay:
					var vnPayRefundResponse = await _vnPayService.RefundAsync(httpContext, new VnPayRefundRequest
					{
						OrderId = returnRequest.OrderId,
						Amount = request.ApprovedRefundAmount,
						PaymentId = originalPaymentId,
						TransactionNo = originalPayment.GatewayTransactionNo,
						TransactionType = request.ApprovedRefundAmount == originalPayment.Amount ? "02" : "03",
						CreateBy = financeAdminId.ToString(),
						OrderInfo = $"Hoàn tiền cho yêu cầu trả hàng {requestId}",
						TransactionDate = originalPayment.CreatedAt.ToString("yyyyMMddHHmmss")
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
						Amount = request.ApprovedRefundAmount,
						PaymentId = originalPaymentId,
						TransactionNo = originalPayment.GatewayTransactionNo,
						Description = $"Hoàn tiền cho yêu cầu trả hàng {requestId}"
					});
					isRefundSuccess = momoRefundResponse.IsSuccess;
					refundMessage = momoRefundResponse.Message;
					refundTransactionNo = momoRefundResponse.TransactionNo;
					break;

				case PaymentMethod.ExternalBankTransfer:
				case PaymentMethod.CashInStore:
					isRefundSuccess = true;
					refundMessage = request.Note ?? "Đã ghi nhận hoàn tiền thủ công bởi quản trị tài chính";
					refundTransactionNo = request.ManualTransactionReference!.Trim();
					break;

				default:
					throw AppException.BadRequest($"Không hỗ trợ hoàn tiền cho phương thức thanh toán {request.RefundMethod}.");
			}

			if (!isRefundSuccess)
			{
				await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					if (pendingRefund == null)
						throw AppException.Internal("Không tìm thấy giao dịch hoàn tiền chờ xử lý.");

					pendingRefund.MarkFailed(refundMessage, refundTransactionNo);
					_unitOfWork.Payments.Update(pendingRefund);
					return true;
				});

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

				if (pendingRefund == null)
					throw AppException.Internal("Không tìm thấy giao dịch hoàn tiền chờ xử lý.");

				pendingRefund.MarkSuccess(refundTransactionNo);
				freshReturnRequest.MarkRefunded(refundTransactionNo);

				_unitOfWork.Orders.Update(order);
				_unitOfWork.Payments.Update(pendingRefund);

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
