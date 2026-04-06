using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
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
		private readonly IVoucherService _voucherService;
		private readonly IOrderPaymentService _orderPaymentService;
		private readonly IOrderShippingHelper _shippingHelper;
		private readonly IOrderDetailsFactory _orderDetailsFactory;
		private readonly IStockReservationService _stockReservationService;
		private readonly IOrderFulfillmentService _fulfillmentService;
		private readonly IContactAddressService _recipientService;
		private readonly INotificationService _notificationService;
		private readonly IGHNService _ghnService;

		public OrderService(
			IUnitOfWork unitOfWork,
			ICartService cartService,
			IVoucherService voucherService,
			IOrderPaymentService orderPaymentService,
			IOrderShippingHelper shippingHelper,
			IOrderDetailsFactory orderDetailsFactory,
			IStockReservationService stockReservationService,
			IOrderFulfillmentService fulfillmentService,
			INotificationService notificationService,
			IContactAddressService recipientService,
			IGHNService ghnService)
		{
			_unitOfWork = unitOfWork;
			_cartService = cartService;
			_voucherService = voucherService;
			_orderPaymentService = orderPaymentService;
			_shippingHelper = shippingHelper;
			_orderDetailsFactory = orderDetailsFactory;
			_stockReservationService = stockReservationService;
			_fulfillmentService = fulfillmentService;
			_notificationService = notificationService;
			_recipientService = recipientService;
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
		public async Task<BaseResponse<OrderResponse>> GetOrderByCodeAsync(string orderCode)
		{
			var order = await _unitOfWork.Orders.GetOrderWithFullDetailsByCodeAsync(orderCode);
			if (order == null)
			{
				return BaseResponse<OrderResponse>.Fail("Order not found or does not belong to user.", ResponseErrorType.NotFound);
			}
			return BaseResponse<OrderResponse>.Ok(order, "Order retrieved successfully.");
		}

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
				   .Select(item => (item.VariantId, item.BatchId, item.Quantity, item.Discount, (decimal?)item.FinalTotal))
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
			if (request.ScannedItems == null || request.ScannedItems.Count == 0)
				throw AppException.BadRequest("No items in the order.");

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				// 1. GOM NHÓM DỮ LIỆU QUÉT
				var groupedScans = request.ScannedItems
					.GroupBy(x => new { x.Barcode, x.BatchCode })
					.Select(g => new { g.Key.Barcode, g.Key.BatchCode, Quantity = g.Sum(item => item.Quantity) })
					.ToList();

				var checkoutItems = new List<CartCheckoutItemDto>();

				// 2. TRUY VẤN DATABASE & KHÓA BATCH_ID
				foreach (var scan in groupedScans)
				{
					var variant = await _unitOfWork.Variants.GetByBarcodeAsync(scan.Barcode)
						?? throw AppException.NotFound($"Variant with barcode {scan.Barcode} not found.");

					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.BatchCode == scan.BatchCode && b.VariantId == variant.Id)
						?? throw AppException.NotFound($"Batch {scan.BatchCode} not found for product {variant.Sku}.");

					var subTotal = variant.BasePrice * scan.Quantity;

					checkoutItems.Add(new CartCheckoutItemDto
					{
						VariantId = variant.Id,
						BatchId = batch.Id, // 💥 BẮT BUỘC CÓ BATCH ID
						VariantName = $"{variant.Sku} - {variant.VolumeMl}ml",
						Quantity = scan.Quantity,
						UnitPrice = variant.BasePrice,
						SubTotal = subTotal,
						Discount = 0m,
						FinalTotal = subTotal,
						BatchCode = batch.BatchCode
					});
				}

				// 3. TÍNH TIỀN CHUNG
				var (pricedItems, subtotal, finalAmount, voucherMessage) = await _cartService.CalculatePricingEngineAsync(
					checkoutItems,
					request.VoucherCode,
					request.CustomerId);

				if (request.ExpectedTotalPrice.HasValue && Math.Abs(request.ExpectedTotalPrice.Value - finalAmount) > 0.0001m)
					throw AppException.Conflict("Price has changed since preview. Please refresh and check the total amount again.");

				// 4. TẠO ĐƠN HÀNG CHỜ (PENDING)
				var order = Order.CreateOffline(request.CustomerId, staffId, finalAmount);

				var factoryItems = pricedItems
					.Select(item => (item.VariantId, item.BatchId, item.Quantity, item.Discount, (decimal?)item.FinalTotal))
					.ToList();

				await _orderDetailsFactory.CreateOrderDetailsAsync(order, factoryItems, finalAmount);
				await _unitOfWork.Orders.AddAsync(order);

				// 5. XỬ LÝ VOUCHER
				if (!string.IsNullOrEmpty(request.VoucherCode))
				{
					var voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode);
					if (voucher != null)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							request.CustomerId, request.Recipient?.ContactPhoneNumber, voucher.Id, order.Id);

						if (markVoucherResult != null) order.AssignVoucher(markVoucherResult);
					}
				}

				if (!request.IsPickupInStore && request.Recipient != null)
				{
					await _shippingHelper.SetupShippingInfoAsync(order, request.Recipient, request.CustomerId, savedAddressId: null);
				}

				// 6. GIỮ KHO CHÍNH XÁC THEO LÔ (EXACT BATCH RESERVATION)
				var posReservationItems = pricedItems
					.GroupBy(x => new { x.VariantId, x.BatchId })
					.Select(g => (VariantId: g.Key.VariantId, BatchId: g.Key.BatchId!.Value, Quantity: g.Sum(x => x.Quantity)))
					.ToList();

				// Set payment expiration
				var paymentExpiresAt = GetPaymentExpiration(request.Payment.Method);

				await _stockReservationService.ReserveExactBatchStockForOrderAsync(order.Id, posReservationItems, paymentExpiresAt);

				// 7. TẠO GIAO DỊCH THANH TOÁN (PENDING)
				var response = await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(order, finalAmount, request.Payment.Method);

				return BaseResponse<string>.Ok(response, "Order created. Waiting for payment confirmation.");
			});
		}
		#endregion Checkout Operations


		#region Order Status Management
		public async Task<BaseResponse<PickListResponse?>> UpdateOrderStatusToPreparingAsync(Guid orderId, Guid staffId)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
					   ?? throw AppException.NotFound("Order not found.");

				   if (order.Status != OrderStatus.Pending)
					   throw AppException.BadRequest($"Order status can only be updated from Pending to Preparing. Current: {order.Status}.");

				   // Update order status
				   order.SetStatus(OrderStatus.Preparing);
				   order.SetStaff(staffId);
				   _unitOfWork.Orders.Update(order);

				   PickListResponse? pickListResponse = null;
				   // Handle Preparing status - return pick list
				   if (order.PaymentStatus == PaymentStatus.Paid || order.PaymentTransactions.Any(p => p.Method == PaymentMethod.CashOnDelivery))
				   {
					   pickListResponse = await _fulfillmentService.GetPickListAsync(order);
				   }

				   var orderTypeText = order.Type == OrderType.Online ? "online" : "in-store";
				   return BaseResponse<PickListResponse?>.Ok(pickListResponse, $"Order is prepared for {orderTypeText} order.");
			   });
		}

		public async Task<BaseResponse<string>> CancelOrderByStaffAsync(Guid orderId, Guid staffId, StaffCancelOrderRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
					?? throw AppException.NotFound("Order not found.");

				if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing && order.Status != OrderStatus.ReadyToPick)
					throw AppException.BadRequest($"Cannot cancel order with status {order.Status}. Only Pending or Preparing orders can be cancelled.");

				var isRefundRequired = order.PaymentStatus == PaymentStatus.Paid;

				if (isRefundRequired)
				{
					var hasPendingCancelRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(
						x => x.OrderId == order.Id && x.Status == CancelRequestStatus.Pending);

					if (hasPendingCancelRequest)
						throw AppException.BadRequest("A pending cancel request already exists for this order.");

					var payload = new CancelRequestPayload
					{
						Reason = request.Reason,
						IsRefundRequired = true,
						RefundAmount = order.TotalAmount,
						StaffNote = request.Note
					};

					var cancelRequest = OrderCancelRequest.Create(order.Id, staffId, payload);
					cancelRequest.Order = order;

					await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);
					return BaseResponse<string>.Ok("Cancel request submitted successfully.");
				}

				await HandleOrderCancellationAsync(order);
				order.SetStaff(staffId);
				_unitOfWork.Orders.Update(order);

				var orderType = order.Type == OrderType.Online ? "online" : "in-store";
				return BaseResponse<string>.Ok($"Order cancelled successfully for {orderType} order.");
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

				   if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing && order.Status != OrderStatus.ReadyToPick)
					   throw AppException.BadRequest($"Cannot cancel order with status {order.Status}. Only Pending or Preparing orders can be cancelled.");

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
		#endregion Order Status Management


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
			return method switch
			{
				PaymentMethod.VnPay => DateTime.UtcNow.AddMinutes(15),
				PaymentMethod.Momo => DateTime.UtcNow.AddMinutes(30),
				_ => DateTime.UtcNow.AddDays(1)
			};
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
		#endregion Private Helper Methods
	}
}
