using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderDetails;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderService : IOrderService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly ICartService _cartService;
		private readonly IVariantService _variantService;
		private readonly IVoucherService _voucherService;
		private readonly IShippingService _shippingService;
		private readonly IOrderPaymentService _orderPaymentService;
		private readonly IOrderInventoryManager _inventoryManager;
		private readonly IOrderShippingHelper _shippingHelper;
		private readonly IOrderValidationService _validationService;
		private readonly IOrderDetailsFactory _orderDetailsFactory;
		private readonly IStockReservationService _stockReservationService;
		private readonly IOrderFulfillmentService _fulfillmentService;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;
		private readonly IRecipientService _recipientService;
		private readonly IAuditScope _auditScope;

		public OrderService(
			IUnitOfWork unitOfWork,
			ICartService cartService,
			IVariantService variantService,
			IVoucherService voucherService,
			IShippingService shippingService,
			IOrderPaymentService orderPaymentService,
			IOrderInventoryManager inventoryManager,
			IOrderShippingHelper shippingHelper,
			IOrderValidationService validationService,
			IOrderDetailsFactory orderDetailsFactory,
			IStockReservationService stockReservationService,
			IOrderFulfillmentService fulfillmentService,
			IRecipientService recipientService,
			IAuditScope auditScope,
			ILoyaltyTransactionService loyaltyTransactionService)
		{
			_unitOfWork = unitOfWork;
			_cartService = cartService;
			_variantService = variantService;
			_voucherService = voucherService;
			_shippingService = shippingService;
			_orderPaymentService = orderPaymentService;
			_inventoryManager = inventoryManager;
			_shippingHelper = shippingHelper;
			_validationService = validationService;
			_orderDetailsFactory = orderDetailsFactory;
			_stockReservationService = stockReservationService;
			_fulfillmentService = fulfillmentService;
			_recipientService = recipientService;
			_auditScope = auditScope;
			_loyaltyTransactionService = loyaltyTransactionService;
		}

		#endregion Dependencies

		public async Task<BaseResponse<string>> UpdateOrderAddressAsync(Guid orderId, Guid userId, UpdateOrderAddressRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// 1. Load order with related data
					var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// Only allow address update before the order is delivering
					if (order.Status >= OrderStatus.Delivering)
					{
						return BaseResponse<string>.Fail(
							"Cannot update address after the order has started delivering.",
							ResponseErrorType.BadRequest);
					}

					// 2. Get existing recipient or create new one
					var existingRecipient = await _unitOfWork.RecipientInfos.GetByOrderIdAsync(order.Id);

					if (existingRecipient == null)
					{
						var setupResult = await _shippingHelper.SetupShippingInfoAsync(
							order.Id,
							request.RecipientInformation,
							userId,
							request.SavedAddressId,
							preCalculatedShippingFee: null,
							orderToUpdate: order);

						if (!setupResult.Success)
						{
							return BaseResponse<string>.Fail(
								setupResult.Message ?? "Failed to setup shipping info.",
								setupResult.ErrorType);
						}

						return BaseResponse<string>.Ok("Order address updated successfully.");
					}

					// 3. Update existing recipient
					var updateResult = await _recipientService.UpdateRecipientInfoAsync(
						existingRecipient,
						request.RecipientInformation,
						request.SavedAddressId,
						userId);

					if (!updateResult.Success)
					{
						return BaseResponse<string>.Fail(
							updateResult.Message,
							updateResult.ErrorType,
							updateResult.Errors);
					}

					// Use the updated recipient info returned from the service as the truth source
					var updatedRecipient = updateResult.Payload!;

					// 4. Update shipping fee
					var shippingInfo = await _unitOfWork.ShippingInfos.GetByOrderIdAsync(order.Id);
					if (shippingInfo != null)
					{
						// Capture old fee before update
						var oldFee = shippingInfo.ShippingFee;

						var feeUpdateResult = await _shippingHelper.UpdateShippingFeeAsync(
							shippingInfo,
							updatedRecipient.DistrictId,
							updatedRecipient.WardCode,
							order);

						if (!feeUpdateResult.Success)
						{
							return BaseResponse<string>.Fail(
								feeUpdateResult.Errors?.FirstOrDefault() ?? feeUpdateResult.Message,
								feeUpdateResult.ErrorType,
								feeUpdateResult.Errors);
						}

						var newFee = feeUpdateResult.Payload;

						if (newFee == oldFee)
						{
							// no-op
						}
						else if (newFee < oldFee && order.CustomerId.HasValue)
						{
							// refund to loyalty points
							var refund = oldFee - newFee;
							const decimal currencyPerPoint = 1000m;
							var points = (int)Math.Floor(refund / currencyPerPoint);
							if (points > 0)
							{
								await _loyaltyTransactionService.PlusPointAsync(order.CustomerId.Value, points, orderId, false);
							}
						}
						else if (newFee > oldFee)
						{
							// increase: add difference to order as COD (rebundant -> COD)
							var diff = newFee - oldFee;
							order.TotalAmount += diff;
							_unitOfWork.Orders.Update(order);
						}
					}

					return BaseResponse<string>.Ok("Order address updated successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred while updating order address: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		#region Query Operations

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request)
		{
			try
			{
				var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request);

				var pagedResult = new PagedResult<OrderListItem>(
					orders,
					request.PageNumber,
					request.PageSize,
					totalCount);

				return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "Orders retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<OrderListItem>>.Fail(
					$"An error occurred while retrieving orders: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId)
		{
			try
			{
				var order = await _unitOfWork.Orders.GetOrderWithFullDetailsAsync(orderId);

				if (order == null)
				{
					return BaseResponse<OrderResponse>.Fail("Order not found.", ResponseErrorType.NotFound);
				}

				return BaseResponse<OrderResponse>.Ok(order, "Order retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<OrderResponse>.Fail(
				$"An error occurred while retrieving order: {ex.Message}",
				ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByStaffIdAsync(Guid staffId, GetPagedOrdersRequest request)
		{
			try
			{
				var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request, staffId: staffId);

				var pagedResult = new PagedResult<OrderListItem>(
					orders,
					request.PageNumber,
					request.PageSize,
					totalCount);

				return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "Staff orders retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<OrderListItem>>.Fail(
					$"An error occurred while retrieving staff orders: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		#endregion Query Operations

		#region User Query Operations

		public async Task<BaseResponse<UserOrderResponse>> GetUserOrderByIdAsync(Guid orderId, Guid userId)
		{
			try
			{
				var order = await _unitOfWork.Orders.GetUserOrderWithFullDetailsAsync(orderId, userId);
				if (order == null)
				{
					return BaseResponse<UserOrderResponse>.Fail("Order not found or does not belong to user.", ResponseErrorType.NotFound);
				}
				return BaseResponse<UserOrderResponse>.Ok(order, "Order retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<UserOrderResponse>.Fail($"An error occurred while retrieving order: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByUserIdAsync(Guid userId, GetPagedOrdersRequest request)
		{
			try
			{
				var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request, userId: userId);

				var pagedResult = new PagedResult<OrderListItem>(
					orders,
					request.PageNumber,
					request.PageSize,
					totalCount);

				return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "User orders retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<PagedResult<OrderListItem>>.Fail(
					$"An error occurred while retrieving user orders: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		#endregion User Query Operations

		#region Checkout Operations

		public async Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request)
		{
			try
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

					var cartResponse = await _cartService.GetCartForCheckoutAsync(userId, getCartTotalRequest);
					if (!cartResponse.Success || cartResponse.Payload == null)
					{
						return BaseResponse<string>.Fail(
						cartResponse.Message ?? "Failed to retrieve cart.",
						cartResponse.ErrorType);
					}

					// Validate voucher if provided
					VoucherResponse? voucher = null;
					if (!string.IsNullOrEmpty(request.VoucherCode))
					{
						var voucherResult = await ValidateAndGetVoucherAsync(request.VoucherCode, userId, null, cartResponse.Payload.TotalPrice);
						if (!voucherResult.Success)
						{
							return BaseResponse<string>.Fail(
								voucherResult.Message ?? "Voucher validation failed.",
								voucherResult.ErrorType);
						}
						voucher = voucherResult.Payload;
					}

					if (cartResponse.Payload.Items.Count == 0)
					{
						return BaseResponse<string>.Fail("Cart is empty.", ResponseErrorType.BadRequest);
					}

					// Create order details
					var itemsToValidate = cartResponse.Payload.Items
					.Select(item => (item.VariantId, item.Quantity))
					.ToList();

					var orderDetailsResult = await _orderDetailsFactory.CreateOrderDetailsAsync(itemsToValidate);
					if (!orderDetailsResult.Success || orderDetailsResult.Payload == null)
					{
						return BaseResponse<string>.Fail(
							orderDetailsResult.Message ?? "Failed to create order details.",
							orderDetailsResult.ErrorType);
					}

					// Set payment expiration
					var paymentExpiresAt = GetPaymentExpiration(request.Payment.Method);

					// Create order
					var order = CreateOnlineOrder(userId, null, cartResponse.Payload.TotalPrice, paymentExpiresAt, orderDetailsResult.Payload);
					await _unitOfWork.Orders.AddAsync(order);

					// Setup shipping if not pickup
					if (request.DeliveryMethod == DeliveryMethod.Delivery)
					{
						var shippingResult = await _shippingHelper.SetupShippingInfoAsync(
							order.Id,
							request.Recipient,
							userId,
							request.SavedAddressId,
							cartResponse.Payload.ShippingFee);

						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}
					}

					// Reserve stock
					var reservationResult = await _stockReservationService.ReserveStockForOrderAsync(
						order.Id,
						itemsToValidate,
						paymentExpiresAt);

					if (!reservationResult.Success)
					{
						return BaseResponse<string>.Fail(
							reservationResult.Message ?? "Stock reservation failed.",
							reservationResult.ErrorType);
					}

					// Mark voucher as reserved
					if (voucher != null)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							userId,
							null,
							voucher.Id,
							order.Id);

						if (!markVoucherResult.Success)
						{
							return BaseResponse<string>.Fail(
								markVoucherResult.Message ?? "Failed to mark voucher as used.",
								markVoucherResult.ErrorType);
						}

						if (markVoucherResult.Payload != null)
						{
							order.UserVoucherId = markVoucherResult.Payload.Id;
							order.UserVoucher = markVoucherResult.Payload;
						}
					}

					// Clear cart Items
					await _cartService.ClearCartAsync(userId, request.ItemIds);

					return await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(
						order.Id,
						cartResponse.Payload.TotalPrice,
						request.Payment.Method,
						"Checkout successful.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred during checkout: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request)
		{
			if (request.OrderDetails.Count == 0)
			{
				return BaseResponse<string>.Fail("No items in the order.", ResponseErrorType.BadRequest);
			}

			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Resolve voucher if provided
					var (voucherCode, voucherId, discount) = await ResolveVoucherAsync(request.VoucherCode);

					// Create order details
					var itemsToValidate = request.OrderDetails
						.Select(od => (od.VariantId, od.Quantity))
						.ToList();

					var orderDetailsResult = await _orderDetailsFactory.CreateOrderDetailsAsync(itemsToValidate);
					if (!orderDetailsResult.Success || orderDetailsResult.Payload == null)
					{
						return BaseResponse<string>.Fail(
							orderDetailsResult.Message ?? "Failed to create order details.",
							orderDetailsResult.ErrorType);
					}

					// Calculate totals
					decimal subtotal = orderDetailsResult.Payload.Sum(od => od.UnitPrice * od.Quantity);
					decimal totalAmount = subtotal;

					if (!string.IsNullOrEmpty(voucherCode))
					{
						var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherCode, subtotal);
						discount = subtotal - discountedTotal;
						totalAmount = discountedTotal;
					}

					// Create order
					var order = CreateOfflineOrder(staffId, null, totalAmount, orderDetailsResult.Payload);
					await _unitOfWork.Orders.AddAsync(order);

					// Mark voucher as reserved and link UserVoucher to order
					if (!string.IsNullOrEmpty(voucherCode) && voucherId.HasValue)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							null,
							null,
							voucherId.Value,
							order.Id);

						if (markVoucherResult.Success)
						{
							var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(
								uv => uv.VoucherId == voucherId.Value && uv.OrderId == order.Id,
								asNoTracking: false);

							if (userVoucher != null)
							{
								order.UserVoucherId = userVoucher.Id;
								order.UserVoucher = userVoucher;
								_unitOfWork.Orders.Update(order);
							}
						}
					}

					// Setup shipping if needed
					if (!request.IsPickupInStore && request.Recipient != null)
					{
						var shippingResult = await _shippingHelper.SetupShippingInfoAsync(
							order.Id,
							request.Recipient,
							null,
							null,
							null,
							order);

						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}

						totalAmount = order.TotalAmount;
					}

					// Deduct inventory immediately for offline orders
					var deductionResult = await _inventoryManager.DeductInventoryAsync(itemsToValidate);
					if (!deductionResult.Success)
					{
						return BaseResponse<string>.Fail(
							deductionResult.Message ?? "Inventory deduction failed.",
							deductionResult.ErrorType);
					}

					return await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(
						order.Id,
						totalAmount,
						request.Payment.Method,
						"In-store checkout successful.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred during in-store checkout: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request)
		{
			try
			{
				if (request.BarCodes.Count == 0)
				{
					return BaseResponse<PreviewOrderResponse>.Fail("No items to preview.", ResponseErrorType.BadRequest);
				}

				var items = await BuildPreviewItemsAsync(request.BarCodes);
				if (!items.Success || items.Payload == null)
				{
					return BaseResponse<PreviewOrderResponse>.Fail(items.Message!, items.ErrorType);
				}

				decimal subtotal = items.Payload.Sum(i => i.Total);
				decimal shippingFee = await CalculateShippingFeeAsync(request.DistrictId, request.WardCode);
				decimal discount = await CalculateVoucherDiscountAsync(request.VoucherCode, subtotal);

				var response = new PreviewOrderResponse
				{
					Items = items.Payload,
					SubTotal = subtotal,
					ShippingFee = shippingFee,
					Discount = discount,
					Total = subtotal + shippingFee - discount
				};

				return BaseResponse<PreviewOrderResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<PreviewOrderResponse>.Fail(
					$"An error occurred while previewing order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		#endregion Checkout Operations

		#region Order Status Management

		public async Task<BaseResponse<PickListResponse>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId);

					if (order == null)
					{
						return BaseResponse<PickListResponse>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// Validate offline order restrictions
					if (order.Type == OrderType.Offline && request.Status != OrderStatus.Canceled)
					{
						return BaseResponse<PickListResponse>.Fail(
							"Offline orders can only be cancelled. Other status updates are not allowed.",
							ResponseErrorType.BadRequest);
					}

					// Validate status transition
					var validationResult = _validationService.ValidateStatusTransition(order.Status, request.Status);
					if (!validationResult.Success)
					{
						return BaseResponse<PickListResponse>.Fail(
							validationResult.Message ?? "Invalid status transition.",
							validationResult.ErrorType);
					}

					// Update order status
					order.Status = request.Status;
					order.StaffId = staffId;
					_unitOfWork.Orders.Update(order);

					// Update shipping status
					UpdateShippingStatus(order, request.Status);

					// Handle Processing status - return pick list
					if (request.Status == OrderStatus.Processing && order.Type == OrderType.Online)
					{
						var pickListResult = await _fulfillmentService.GetPickListAsync(order.Id);
						if (pickListResult.Success && pickListResult.Payload != null)
						{
							return BaseResponse<PickListResponse>.Ok(
								pickListResult.Payload,
								"Order is now in Picking phase. Use FulfillOrderAsync to complete.");
						}
					}

					// Handle cancellation
					if (request.Status == OrderStatus.Canceled)
					{
						await HandleOrderCancellationAsync(order);
						var isRefundRequired = order.PaymentStatus == PaymentStatus.Paid;

						var cancelRequest = new OrderCancelRequest
						{
							OrderId = order.Id,
							RequestedById = staffId,
							ProcessedById = null,
							Reason = request.Note ?? "Staff cancelled order.",
							StaffNote = request.Note,
							Status = CancelRequestStatus.Pending,
							IsRefundRequired = isRefundRequired,
							RefundAmount = isRefundRequired ? order.TotalAmount : null,
							IsRefunded = false,
							Order = order
						};
						await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);
					}

					// Handle delivery completion - update loyalty points
					if (request.Status == OrderStatus.Delivered)
					{
						// Award loyalty points
						if (order.CustomerId.HasValue)
						{
							using (_auditScope.BeginSystemAction())
							{
								int pointsToAward = (int)(order.TotalAmount * 0.01m);
								if (pointsToAward > 0)
								{
									await _loyaltyTransactionService.PlusPointAsync(order.CustomerId.Value, pointsToAward, orderId, false);
								}
							}
						}
					}

					var orderTypeText = order.Type == OrderType.Online ? "online" : "in-store";
					return BaseResponse<PickListResponse>.Ok(
						new PickListResponse(),
						$"Order status updated to {request.Status} for {orderTypeText} order.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<PickListResponse>.Fail(
					$"An error occurred while updating order status: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId, UserCancelOrderRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(orderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// Validate authorization
					if (order.CustomerId != userId)
					{
						return BaseResponse<string>.Fail(
					"You are not authorized to cancel this order.",
					ResponseErrorType.Forbidden);
					}

					// Validate order type
					if (order.Type != OrderType.Online)
					{
						return BaseResponse<string>.Fail(
					"Only online orders can be cancelled.",
					ResponseErrorType.BadRequest);
					}

					// Validate status
					if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
					{
						return BaseResponse<string>.Fail(
					$"Cannot cancel order with status {order.Status}. Only Pending or Processing orders can be cancelled.",
					ResponseErrorType.BadRequest);
					}

					if (order.Status == OrderStatus.Canceled)
					{
						return BaseResponse<string>.Fail("Order is already cancelled.", ResponseErrorType.BadRequest);
					}

					// Create OrderCancelRequest
					bool isRefundRequired = order.PaymentStatus == PaymentStatus.Paid;
					var cancelRequest = new OrderCancelRequest
					{
						OrderId = order.Id,
						RequestedById = userId,
						ProcessedById = isRefundRequired ? null : userId,
						Reason = request.Reason ?? "Customer cancelled order.",
						Status = CancelRequestStatus.Pending,
						IsRefundRequired = isRefundRequired,
						RefundAmount = isRefundRequired ? order.TotalAmount : null,
						IsRefunded = false,
						Order = order
					};
					await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);

					return BaseResponse<string>.Ok("Cancel request submitted successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred while cancelling order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		#endregion

		#region Fulfillment Operations (Delegated)

		public Task<BaseResponse<PickListResponse>> GetOrderPickListAsync(Guid orderId)
		{
			return _fulfillmentService.GetPickListAsync(orderId);
		}

		public Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			return _fulfillmentService.FulfillOrderAsync(orderId, staffId, request);
		}

		public Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request)
		{
			return _fulfillmentService.SwapDamagedStockAsync(orderId, staffId, request);
		}

		#endregion

		#region Private Helper Methods

		private static DateTime GetPaymentExpiration(PaymentMethod method)
		{
			return method == PaymentMethod.VnPay
				? DateTime.UtcNow.AddMinutes(15)
				: DateTime.UtcNow.AddDays(1);
		}

		private static Order CreateOnlineOrder(Guid customerId, Guid? userVoucherId, decimal totalAmount, DateTime paymentExpiresAt, List<OrderDetail> orderDetails)
		{
			return new Order
			{
				CustomerId = customerId,
				Type = OrderType.Online,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				UserVoucherId = userVoucherId,
				TotalAmount = totalAmount,
				PaymentExpiresAt = paymentExpiresAt,
				OrderDetails = orderDetails
			};
		}

		private static Order CreateOfflineOrder(Guid staffId, Guid? userVoucherId, decimal totalAmount, List<OrderDetail> orderDetails)
		{
			return new Order
			{
				CustomerId = null,
				StaffId = staffId,
				Type = OrderType.Offline,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				UserVoucherId = userVoucherId,
				TotalAmount = totalAmount,
				OrderDetails = orderDetails
			};
		}

		private async Task<(string? VoucherCode, Guid? VoucherId, decimal Discount)> ResolveVoucherAsync(string? voucherCode)
		{
			if (string.IsNullOrEmpty(voucherCode))
			{
				return (null, null, 0m);
			}

			var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode)
				?? throw new InvalidOperationException("Invalid voucher code.");

			if (voucher.ExpiryDate < DateTime.UtcNow)
			{
				throw new InvalidOperationException("Voucher has expired.");
			}

			return (voucher.Code, voucher.Id, 0m);
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

		private async Task<decimal> CalculateShippingFeeAsync(int districtId, string? wardCode)
		{
			if (string.IsNullOrEmpty(wardCode) || districtId <= 0)
			{
				return 0;
			}

			return await _shippingService.CalculateShippingFeeAsync(districtId, wardCode) ?? 0;
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
			if (order.ShippingInfo == null) return;

			var shippingStatus = _shippingHelper.MapOrderStatusToShippingStatus(newStatus);
			if (shippingStatus.HasValue)
			{
				order.ShippingInfo.Status = shippingStatus.Value;
				_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
			}
		}

		private async Task HandleOrderCancellationAsync(Order order)
		{
			if (order.Type == OrderType.Online)
			{
				await _stockReservationService.ReleaseReservationAsync(order.Id);
			}
			else
			{
				var itemsToRestore = order.OrderDetails
					.Select(od => (od.VariantId, od.Quantity))
					.ToList();

				await _inventoryManager.RestoreInventoryAsync(itemsToRestore);
			}

			await ReleaseVoucherIfUsedAsync(order);
		}

		private async Task ReleaseVoucherIfUsedAsync(Order order)
		{
			if (!order.UserVoucherId.HasValue || !order.CustomerId.HasValue) return;

			var userVoucher = order.UserVoucher
				?? await _unitOfWork.UserVouchers.GetByIdAsync(order.UserVoucherId.Value);

			if (userVoucher == null) return;

			await _voucherService.ReleaseReservedVoucherAsync(order.Id);
		}

		private async Task<BaseResponse<VoucherResponse>> ValidateAndGetVoucherAsync(string voucherCode, Guid userId, string? phoneNumber, decimal totalPrice)
		{
			// Validate voucher eligibility
			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(voucherCode, userId, totalPrice, phoneNumber);
			if (!voucherValidation.Success)
			{
				return BaseResponse<VoucherResponse>.Fail(
					voucherValidation.Message ?? "Voucher validation failed.",
					voucherValidation.ErrorType);
			}

			// Get voucher details
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode);
			if (voucher == null)
			{
				return BaseResponse<VoucherResponse>.Fail(
					"Voucher not found.",
					ResponseErrorType.NotFound);
			}

			return BaseResponse<VoucherResponse>.Ok(voucher);
		}

		#endregion
	}
}
