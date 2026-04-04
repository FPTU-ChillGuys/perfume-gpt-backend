using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders.OrderDetails;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.OrderCancelRequest;

namespace PerfumeGPT.Application.Services
{
	public class OrderService : IOrderService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICartService _cartService;
		private readonly IVariantService _variantService;
		private readonly IVoucherService _voucherService;
		private readonly IOrderPaymentService _orderPaymentService;
		private readonly IOrderInventoryManager _inventoryManager;
		private readonly IOrderShippingHelper _shippingHelper;
		private readonly IOrderDetailsFactory _orderDetailsFactory;
		private readonly IStockReservationService _stockReservationService;
		private readonly IOrderFulfillmentService _fulfillmentService;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;
		private readonly IContactAddressService _recipientService;
		private readonly INotificationService _notificationService;
		private readonly IAuditScope _auditScope;
		private readonly IGHNService _ghnService;

		public OrderService(
			IUnitOfWork unitOfWork,
			ICartService cartService,
			IVariantService variantService,
			IVoucherService voucherService,
			IOrderPaymentService orderPaymentService,
			IOrderInventoryManager inventoryManager,
			IOrderShippingHelper shippingHelper,
			IOrderDetailsFactory orderDetailsFactory,
			IStockReservationService stockReservationService,
			IOrderFulfillmentService fulfillmentService,
			INotificationService notificationService,
			IContactAddressService recipientService,
			IAuditScope auditScope,
		   ILoyaltyTransactionService loyaltyTransactionService,
			IGHNService ghnService)
		{
			_unitOfWork = unitOfWork;
			_cartService = cartService;
			_variantService = variantService;
			_voucherService = voucherService;
			_orderPaymentService = orderPaymentService;
			_inventoryManager = inventoryManager;
			_shippingHelper = shippingHelper;
			_orderDetailsFactory = orderDetailsFactory;
			_stockReservationService = stockReservationService;
			_fulfillmentService = fulfillmentService;
			_notificationService = notificationService;
			_recipientService = recipientService;
			_auditScope = auditScope;
			_loyaltyTransactionService = loyaltyTransactionService;
			_ghnService = ghnService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> UpdateOrderAddressAsync(Guid orderId, Guid userId, UpdateOrderAddressRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   // 1. Load order with related data
				   var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
					   ?? throw AppException.NotFound("Order not found.");

				   order.EnsureAddressUpdatable();

				   // 2. Get existing recipient or create new one
				   if (order.ContactAddress == null)
				   {
					   await _shippingHelper.SetupShippingInfoAsync(order, request.RecipientInformation, userId, request.SavedAddressId);
					   return BaseResponse<string>.Ok("Order address updated successfully.");
				   }

				   // 3. Update existing recipient
				   var updateResult = await _recipientService.UpdateContactAddressAsync(order.ContactAddress, request.RecipientInformation, request.SavedAddressId, userId);

				   return BaseResponse<string>.Ok("Order address updated successfully.");
			   });
		}


		#region Query Operations
		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request)
		{
			var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request);

			var pagedResult = new PagedResult<OrderListItem>(
				orders,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "Orders retrieved successfully.");
		}

		public async Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderWithFullDetailsAsync(orderId)
				   ?? throw AppException.NotFound("Order not found.");

			return BaseResponse<OrderResponse>.Ok(order, "Order retrieved successfully.");
		}

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByStaffIdAsync(Guid staffId, GetPagedOrdersRequest request)
		{
			var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request, staffId: staffId);

			var pagedResult = new PagedResult<OrderListItem>(
				orders,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "Staff orders retrieved successfully.");
		}

		public async Task<BaseResponse<ReceiptResponse>> GetInvoiceAsync(Guid orderId)
		{
			var invoice = await _unitOfWork.Orders.GetInvoiceAsync(orderId)
				?? throw AppException.NotFound("Order invoice not found.");

			return BaseResponse<ReceiptResponse>.Ok(invoice, "Order invoice retrieved successfully.");
		}
		#endregion Query Operations


		#region User Query Operations
		public async Task<BaseResponse<UserOrderResponse>> GetUserOrderByIdAsync(Guid orderId, Guid userId)
		{
			var order = await _unitOfWork.Orders.GetUserOrderWithFullDetailsAsync(orderId, userId);
			if (order == null)
			{
				return BaseResponse<UserOrderResponse>.Fail("Order not found or does not belong to user.", ResponseErrorType.NotFound);
			}
			return BaseResponse<UserOrderResponse>.Ok(order, "Order retrieved successfully.");
		}

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByUserIdAsync(Guid userId, GetPagedOrdersRequest request)
		{
			var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request, userId: userId);

			var pagedResult = new PagedResult<OrderListItem>(
				orders,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "User orders retrieved successfully.");
		}

		public async Task<BaseResponse<ReceiptResponse>> GetMyInvoiceAsync(Guid orderId, Guid userId)
		{
			var invoice = await _unitOfWork.Orders.GetUserInvoiceAsync(orderId, userId);
			if (invoice == null)
			{
				return BaseResponse<ReceiptResponse>.Fail("Order invoice not found or does not belong to user.", ResponseErrorType.NotFound);
			}

			return BaseResponse<ReceiptResponse>.Ok(invoice, "Order invoice retrieved successfully.");
		}
		#endregion User Query Operations


		#region Checkout Operations
		public async Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				// Get cart with items
				var getCartTotalRequest = new GetCartTotalRequest
				{
					VoucherCode = request.VoucherCode,
					ItemIds = request.ItemIds,
					SavedAddressId = request.SavedAddressId,
					Recipient = request.Recipient
				};

				var cartResponse = await _cartService.GetCartForCheckoutAsync(userId, getCartTotalRequest)
					?? throw AppException.BadRequest("Failed to retrieve cart for checkout.");

				if (cartResponse.Items.Count == 0)
					throw AppException.BadRequest("Cart is empty.");

				if (request.ExpectedTotalPrice.HasValue && Math.Abs(request.ExpectedTotalPrice.Value - cartResponse.TotalPrice) > 0.0001m)
					throw AppException.Conflict("Discount no longer matches what you saw. Please refresh cart total and checkout again.");

				// Create order details
				var itemsToValidate = cartResponse.Items
				.Select(item => (item.VariantId, item.Quantity))
				.ToList();

				var pricedItems = cartResponse.Items
					.Select(item => (item.VariantId, item.Quantity, item.Discount))
					.ToList();

				// Set payment expiration
				var paymentExpiresAt = GetPaymentExpiration(request.Payment.Method);

				// Create order and populate details through aggregate methods
				var order = Order.CreateOnline(userId, cartResponse.TotalPrice, paymentExpiresAt);
				await _orderDetailsFactory.CreateOrderDetailsAsync(order, pricedItems, cartResponse.TotalPrice);

				// Validate voucher if provided (with subtotal + cart variant context)
				VoucherResponse? voucher = null;

				if (!string.IsNullOrEmpty(request.VoucherCode))
				{
					var subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
					voucher = await ValidateAndGetVoucherAsync(
						request.VoucherCode,
						userId,
						null,
						subtotal,
						itemsToValidate.Select(x => x.VariantId));
				}

				// Recalculate total right before reservation to detect promotion/batch drift
				var latestTotalResponse = await _cartService.GetCartTotalAsync(userId, getCartTotalRequest);
				if (!latestTotalResponse.Success || latestTotalResponse.Payload == null)
					throw AppException.BadRequest(latestTotalResponse.Message ?? "Failed to refresh cart total.");

				if (Math.Abs(latestTotalResponse.Payload.TotalPrice - cartResponse.TotalPrice) > 0.0001m)
					throw AppException.Conflict("Discount no longer matches what you saw. Please refresh cart total and checkout again.");

				// Persist order
				await _unitOfWork.Orders.AddAsync(order);

				// Setup shipping if not pickup
				if (request.DeliveryMethod == DeliveryMethod.Delivery)
				{
					await _shippingHelper.SetupShippingInfoAsync(order, request.Recipient, userId, request.SavedAddressId);
				}

				// Reserve stock
				await _stockReservationService.ReserveStockForOrderAsync(order.Id, itemsToValidate, paymentExpiresAt);

				// Mark voucher as reserved
				if (voucher != null)
				{
					var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(userId, null, voucher.Id, order.Id)
						?? throw AppException.BadRequest("Failed to mark voucher as used.");

					if (markVoucherResult != null)
						order.AssignVoucher(markVoucherResult);
				}

				// Clear cart Items
				await _cartService.ClearCartAsync(userId, request.ItemIds);

				var response = await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(order, cartResponse.TotalPrice, request.Payment.Method);
				await _notificationService.CreateNewOrderNotificationAsync(order.Id, cartResponse.TotalPrice);

				return BaseResponse<string>.Ok(response, "Checkout successful.");
			});
		}

		public async Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request)
		{
			if (request.OrderDetails.Count == 0)
				throw AppException.BadRequest("No items in the order.");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				// Resolve voucher if provided
				VoucherResponse? voucher = null;
				if (!string.IsNullOrEmpty(request.VoucherCode))
				{
					voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode)
						?? throw AppException.BadRequest("Invalid voucher code.");

					if (voucher.ExpiryDate < DateTime.UtcNow)
						throw AppException.BadRequest("Voucher has expired.");
				}

				// Create order details
				var itemsToValidate = request.OrderDetails
				  .Select(od => (od.VariantId, od.Quantity))
					.ToList();

				var pricedItems = request.OrderDetails
					.Select(od => (od.VariantId, od.Quantity, 0m))
					.ToList();

				var order = Order.CreateOffline(staffId, 0);
				await _orderDetailsFactory.CreateOrderDetailsAsync(order, pricedItems);

				// Calculate totals
				decimal subtotal = order.OrderDetails.Sum(od => od.UnitPrice * od.Quantity);
				decimal totalAmount = subtotal;

				if (voucher != null)
				{
					var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucher.Code, subtotal);
					totalAmount = discountedTotal;
				}

				order.SetTotalAmount(totalAmount);

				// Persist order
				await _unitOfWork.Orders.AddAsync(order);

				// Mark voucher as reserved and link UserVoucher to order
				if (voucher != null)
				{
					var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
						null,
						request.Recipient?.ContactPhoneNumber,
						voucher.Id,
						order.Id);

					if (markVoucherResult != null)
					{
						var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
							uv => uv.VoucherId == voucher.Id && uv.OrderId == order.Id);

						if (userVoucher != null)
						{
							order.AssignVoucher(userVoucher);
							_unitOfWork.Orders.Update(order);
						}
					}

					await _voucherService.MarkVoucherAsUsedAsync(voucher.Id);
				}

				// Setup shipping if needed
				if (!request.IsPickupInStore && request.Recipient != null)
				{
					await _shippingHelper.SetupShippingInfoAsync(order, request.Recipient, customerId: null, savedAddressId: null);

					totalAmount = order.TotalAmount;
				}

				// Deduct inventory immediately for offline orders
				await _inventoryManager.DeductInventoryAsync(itemsToValidate);

				var response = await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(
					order,
					totalAmount,
					request.Payment.Method);

				return BaseResponse<string>.Ok(response, "In-store checkout successful.");
			});
		}

		public async Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request)
		{
			if (request.BarCodes.Count == 0)
				throw AppException.BadRequest("No items to preview.");

			var items = await BuildPreviewItemsAsync(request.BarCodes);
			if (!items.Success || items.Payload == null)
				throw AppException.BadRequest(items.Message!);

			decimal subtotal = items.Payload.Sum(i => i.Total);
			decimal discount = await CalculateVoucherDiscountAsync(request.VoucherCode, subtotal);

			var response = new PreviewOrderResponse
			{
				Items = items.Payload,
				SubTotal = subtotal,
				ShippingFee = 0,
				Discount = discount,
				Total = subtotal + 0 - discount
			};

			return BaseResponse<PreviewOrderResponse>.Ok(response);
		}

		#endregion Checkout Operations


		#region Order Status Management

		public async Task<BaseResponse<PickListResponse?>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
					   ?? throw AppException.NotFound("Order not found.");

				   // Validate offline order restrictions
				   if (order.Type == OrderType.Offline && request.Status != OrderStatus.Cancelled)
					   throw AppException.BadRequest("Offline orders can only be cancelled. Other status updates are not allowed.");

				   // Handle cancellation first to align refund/no-refund flows
				   if (request.Status == OrderStatus.Cancelled)
				   {
					   var isRefundRequired = order.PaymentStatus == PaymentStatus.Paid;

					   // Paid cancellation => create cancel request, wait for approval/refund flow
					   if (isRefundRequired)
					   {
						   var hasPendingCancelRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(
							   x => x.OrderId == order.Id && x.Status == CancelRequestStatus.Pending);

						   if (hasPendingCancelRequest)
							   throw AppException.BadRequest("A pending cancel request already exists for this order.");

						   var payload = new CancelRequestPayload
						   {
							   Reason = CancelOrderReason.InsufficientStock,
							   IsRefundRequired = true,
							   RefundAmount = order.TotalAmount,
							   StaffNote = request.Note
						   };

						   var cancelRequest = OrderCancelRequest.Create(order.Id, staffId, payload);
						   cancelRequest.Order = order;

						   await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);

						   return BaseResponse<PickListResponse?>.Ok(null, "Cancel request submitted successfully.");
					   }

					   // staff cancels without refund => direct cancel
					   await HandleOrderCancellationAsync(order);
					   order.SetStaff(staffId);
					   _unitOfWork.Orders.Update(order);

					   var orderType = order.Type == OrderType.Online ? "online" : "in-store";
					   return BaseResponse<PickListResponse?>.Ok(null, $"Order status updated to {request.Status} for {orderType} order.");
				   }

				   // return order handle

				   if (request.Status == OrderStatus.Delivered)
					   throw AppException.BadRequest("Delivered status is synchronized from shipping provider. Please run shipping sync instead.");

				   // Update order status
				   order.SetStatus(request.Status);
				   order.SetStaff(staffId);
				   _unitOfWork.Orders.Update(order);

				   // Update shipping status
				   UpdateShippingStatus(order, request.Status);

				   PickListResponse? pickListResponse = null;
				   // Handle Processing status - return pick list
				   if (request.Status == OrderStatus.Processing && order.Type == OrderType.Online)
				   {
					   pickListResponse = await _fulfillmentService.GetPickListAsync(order);
				   }

				   var orderTypeText = order.Type == OrderType.Online ? "online" : "in-store";
				   return BaseResponse<PickListResponse?>.Ok(pickListResponse, $"Order status updated to {request.Status} for {orderTypeText} order.");
			   });
		}

		public async Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId, UserCancelOrderRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(orderId)
					   ?? throw AppException.NotFound("Order not found.");

				   // Validate authorization
				   order.EnsureOwnedBy(userId);

				   // Validate order type
				   order.EnsureOnlineOrder();

				   if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
					   throw AppException.BadRequest($"Cannot cancel order with status {order.Status}. Only Pending or Processing orders can be cancelled.");

				   var isRefundRequired = order.PaymentStatus == PaymentStatus.Paid;

				   // Pending + Unpaid => auto cancel immediately
				   if (order.Status == OrderStatus.Pending && !isRefundRequired)
				   {
					   await HandleOrderCancellationAsync(order);
					   _unitOfWork.Orders.Update(order);

					   return BaseResponse<string>.Ok("Order cancelled successfully.");
				   }

				   var hasPendingCancelRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(
					   x => x.OrderId == order.Id && x.Status == CancelRequestStatus.Pending);

				   if (hasPendingCancelRequest)
					   throw AppException.BadRequest("A pending cancel request already exists for this order.");

				   var payload = new CancelRequestPayload
				   {
					   Reason = request.Reason,
					   IsRefundRequired = isRefundRequired,
					   RefundAmount = isRefundRequired ? order.TotalAmount : null
				   };

				   var cancelRequest = OrderCancelRequest.Create(order.Id, userId, payload);
				   cancelRequest.Order = order;

				   await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);

				   return BaseResponse<string>.Ok("Cancel request submitted successfully.");
			   });
		}
		#endregion


		#region Fulfillment Operations
		public async Task<BaseResponse<PickListResponse>> GetOrderPickListAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderForPickListAsync(orderId)
				?? throw AppException.NotFound("Order not found.");
			return BaseResponse<PickListResponse>.Ok(await _fulfillmentService.GetPickListAsync(order));
		}

		public async Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			return BaseResponse<string>.Ok(await _fulfillmentService.FulfillOrderAsync(orderId, staffId, request));
		}

		public async Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request)
		{
			return BaseResponse<SwapDamagedStockResponse>.Ok(await _fulfillmentService.SwapDamagedStockAsync(orderId, staffId, request));
		}
		#endregion Fulfillment Operations


		#region Private Helper Methods
		private static DateTime GetPaymentExpiration(PaymentMethod method)
		{
			return method == PaymentMethod.VnPay
				? DateTime.UtcNow.AddMinutes(15)
				: DateTime.UtcNow.AddDays(1);
		}

		private async Task<BaseResponse<List<OrderDetailListItems>>> BuildPreviewItemsAsync(List<string> barCodes)
		{
			var items = new List<OrderDetailListItems>();

			foreach (var barcode in barCodes.Distinct())
			{
				var variantResponse = await _variantService.GetVariantByBarcodeAsync(barcode);
				if (!variantResponse.Success || variantResponse.Payload == null)
				{
					return BaseResponse<List<OrderDetailListItems>>.Fail(
						$"Product with barcode {barcode} not found.",
						ResponseErrorType.NotFound);
				}

				var variant = variantResponse.Payload;
				var quantity = barCodes.Count(b => b == barcode);
				var itemTotal = variant.BasePrice * quantity;

				items.Add(new OrderDetailListItems
				{
					VariantId = variant.Id,
					VariantName = $"{variant.Sku} - {variant.VolumeMl}ml - {variant.ConcentrationName} - {variant.Type}",
					ImageUrl = variant.Media?.FirstOrDefault(m => m.IsPrimary)?.Url ?? string.Empty,
					Quantity = quantity,
					Total = (int)itemTotal
				});
			}

			return BaseResponse<List<OrderDetailListItems>>.Ok(items);
		}

		private async Task<decimal> CalculateVoucherDiscountAsync(string? voucherCode, decimal subtotal)
		{
			if (string.IsNullOrEmpty(voucherCode))
			{
				return 0;
			}

			var voucherResponse = await _voucherService.GetVoucherByCodeAsync(voucherCode);
			if (voucherResponse == null)
			{
				return 0;
			}

			var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherResponse.Code, subtotal);
			return subtotal - discountedTotal;
		}

		private void UpdateShippingStatus(Order order, OrderStatus newStatus)
		{
			if (order.ForwardShipping == null) return;

			var shippingInfo = order.ForwardShipping;

			var mappedStatus = _shippingHelper.MapOrderStatusToShippingStatus(newStatus);
			if (!mappedStatus.HasValue)
				return;

			switch (mappedStatus.Value)
			{
				case ShippingStatus.Pending:
					break;
				case ShippingStatus.Delivering:
					shippingInfo.MarkAsDelivering();
					break;
				case ShippingStatus.Delivered:
					if (shippingInfo.Status != ShippingStatus.Delivering)
						shippingInfo.MarkAsDelivering();
					shippingInfo.MarkAsDelivered();
					break;
				case ShippingStatus.Cancelled:
					shippingInfo.Cancel();
					break;
				case ShippingStatus.Returned:
					shippingInfo.MarkAsReturned();
					break;
			}

			_unitOfWork.ShippingInfos.Update(shippingInfo);
		}

		private async Task HandleOrderCancellationAsync(Order order)
		{
			if (!string.IsNullOrWhiteSpace(order.ForwardShipping?.TrackingNumber))
			{
				await _ghnService.CancelOrderAsync(new CancelOrderRequest
				{
					TrackingNumbers = [order.ForwardShipping.TrackingNumber]
				});
			}

			order.SetStatus(OrderStatus.Cancelled);
			UpdateShippingStatus(order, OrderStatus.Cancelled);
			await _stockReservationService.ReleaseReservationAsync(order.Id);
			await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);
		}

		private async Task<VoucherResponse> ValidateAndGetVoucherAsync(
			  string voucherCode,
			  Guid userId,
			  string? phoneNumber,
			  decimal totalPrice,
			  IEnumerable<Guid>? cartVariantIds = null)
		{
			// Get voucher details
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode)
				?? throw AppException.NotFound("Voucher not found.");

			// Validate voucher eligibility
			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(voucherCode, userId, totalPrice, phoneNumber, cartVariantIds);
			if (!voucherValidation) throw AppException.BadRequest("Voucher validation failed.");

			return voucher;
		}
		#endregion
	}
}
