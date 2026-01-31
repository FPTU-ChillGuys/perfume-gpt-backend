using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderDetails;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderService : IOrderService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICartService _cartService;
		private readonly IVariantService _variantService;
		private readonly IVoucherService _voucherService;
		private readonly IShippingService _shippingService;
		private readonly IValidator<CreateOrderRequest> _createOrderValidator;
		private readonly IOrderPaymentService _orderPaymentService;
		private readonly IOrderInventoryManager _inventoryManager;
		private readonly IOrderShippingHelper _shippingHelper;
		private readonly IOrderValidationService _validationService;
		private readonly IOrderDetailsFactory _orderDetailsFactory;
		private readonly IStockReservationService _stockReservationService;

		public OrderService(
			IUnitOfWork unitOfWork,
			ICartService cartService,
			IVariantService variantService,
			IVoucherService voucherService,
			IShippingService shippingService,
			IValidator<CreateOrderRequest> createOrderValidator,
			IOrderPaymentService orderPaymentService,
			IOrderInventoryManager inventoryManager,
			IOrderShippingHelper shippingHelper,
			IOrderValidationService validationService,
			IOrderDetailsFactory orderDetailsFactory,
			IStockReservationService stockReservationService)
		{
			_unitOfWork = unitOfWork;
			_cartService = cartService;
			_variantService = variantService;
			_voucherService = voucherService;
			_shippingService = shippingService;
			_createOrderValidator = createOrderValidator;
			_orderPaymentService = orderPaymentService;
			_inventoryManager = inventoryManager;
			_shippingHelper = shippingHelper;
			_validationService = validationService;
			_orderDetailsFactory = orderDetailsFactory;
			_stockReservationService = stockReservationService;
		}

		public async Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request)
		{
			var validationResult = await _createOrderValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)),
					ResponseErrorType.BadRequest);
			}

			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Validate voucher if provided
					if (request.VoucherId.HasValue && request.VoucherId.Value != Guid.Empty)
					{
						var voucherValidation = await _voucherService.ValidateToApplyVoucherAsync(
							request.VoucherId.Value,
							userId);

						if (!voucherValidation.Success)
						{
							return BaseResponse<string>.Fail(
								voucherValidation.Message ?? "Voucher validation failed.",
								voucherValidation.ErrorType);
						}
					}

					var cartResponse = await _cartService.GetCartByUserIdAsync(userId, request.VoucherId);
					if (!cartResponse.Success || cartResponse.Payload == null)
					{
						return BaseResponse<string>.Fail(
							cartResponse.Message ?? "Failed to retrieve cart.",
							cartResponse.ErrorType);
					}

					if (cartResponse.Payload.Items.Count == 0)
					{
						return BaseResponse<string>.Fail("Cart is empty.", ResponseErrorType.BadRequest);
					}

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

					// Set payment expiration based on payment method
					var paymentExpiresAt = DateTime.UtcNow.AddDays(1); // default payment expiration time for staff to process
					if (request.Payment.Method == PaymentMethod.VnPay)
					{
						paymentExpiresAt = DateTime.UtcNow.AddMinutes(15); // vnpay payment expiration time
					}

					var order = new Order
					{
						CustomerId = userId,
						Type = OrderType.Online,
						Status = OrderStatus.Pending,
						PaymentStatus = PaymentStatus.Unpaid,
						ExternalShopeeId = request.ExternalShopeeId,
						VoucherId = request.VoucherId,
						TotalAmount = cartResponse.Payload.TotalPrice,
						PaymentExpiresAt = paymentExpiresAt,
						OrderDetails = orderDetailsResult.Payload
					};

					await _unitOfWork.Orders.AddAsync(order);
					// Don't save yet - let transaction handle it at the end

					if (!request.IsPickupInStore)
					{
						var shippingResult = await _shippingHelper.SetupShippingInfoAsync(
							order.Id,
							request.Recipient,
							userId,
							cartResponse.Payload.ShippingFee);

						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}
					}

					// Reserve stock instead of deducting immediately (for online orders)
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

					// Mark voucher as reserved if provided
					if (request.VoucherId.HasValue && request.VoucherId.Value != Guid.Empty)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							userId,
							request.VoucherId.Value);

						if (!markVoucherResult.Success)
						{
							return BaseResponse<string>.Fail(
								markVoucherResult.Message ?? "Failed to mark voucher as used.",
								markVoucherResult.ErrorType);
						}
					}

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
					// Resolve and validate voucher if provided
					Guid? voucherId = null;
					decimal discount = 0m;

					if (!string.IsNullOrEmpty(request.VoucherCode))
					{
						var voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode);
						if (voucher == null)
						{
							return BaseResponse<string>.Fail(
								"Invalid voucher code.",
								ResponseErrorType.NotFound);
						}

						if (voucher.ExpiryDate < DateTime.UtcNow)
						{
							return BaseResponse<string>.Fail(
								"Voucher has expired.",
								ResponseErrorType.BadRequest);
						}

						voucherId = voucher.Id;
					}

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

					decimal subtotal = orderDetailsResult.Payload.Sum(od => od.UnitPrice * od.Quantity);
					decimal totalAmount = subtotal;

					// Apply voucher discount if voucher is valid
					if (voucherId.HasValue)
					{
						var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherId.Value, subtotal);
						discount = subtotal - discountedTotal;
						totalAmount = discountedTotal;
					}

					var order = new Order
					{
						CustomerId = null,
						StaffId = staffId,
						Type = OrderType.Offline,
						Status = OrderStatus.Pending,
						PaymentStatus = PaymentStatus.Unpaid,
						VoucherId = voucherId,
						TotalAmount = totalAmount,
						OrderDetails = orderDetailsResult.Payload
					};

					await _unitOfWork.Orders.AddAsync(order);
					// Don't save yet - let transaction handle it at the end

					if (!request.IsPickupInStore && request.Recipient != null)
					{
						var shippingResult = await _shippingHelper.SetupShippingInfoAsync(
							order.Id,
							request.Recipient,
							null,
							null,
							order);

						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}

						totalAmount = order.TotalAmount; // Order was updated by SetupShippingInfoAsync
					}

					var deductionResult = await _inventoryManager.DeductInventoryAsync(itemsToValidate);
					if (!deductionResult.Success)
					{
						return BaseResponse<string>.Fail(
							deductionResult.Message ?? "Inventory deduction failed.",
							deductionResult.ErrorType);
					}

					// Note: We don't mark voucher as reserved for offline orders since there's no customer
					// The voucher discount is applied but not tracked per user

					return await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(
						order.Id,
						totalAmount,
						request.Payment.Method,
						"In-store checkout successful.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"An error occurred during in-store checkout: {ex.Message}", ResponseErrorType.InternalError);
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

				var items = new List<OrderDetailListItems>();
				decimal subtotal = 0;

				foreach (var barcode in request.BarCodes)
				{
					var variantResponse = await _variantService.GetVariantByBarcodeAsync(barcode);
					if (!variantResponse.Success || variantResponse.Payload == null)
					{
						return BaseResponse<PreviewOrderResponse>.Fail($"Product with barcode {barcode} not found.", ResponseErrorType.NotFound);
					}

					var variant = variantResponse.Payload;
					var quantity = request.BarCodes.Count(b => b == barcode);
					var itemTotal = variant.BasePrice * quantity;
					subtotal += itemTotal;

					if (!items.Any(i => i.VariantId == variant.Id))
					{
						items.Add(new OrderDetailListItems
						{
							VariantId = variant.Id,
							VariantName = $"{variant.Sku} - {variant.VolumeMl}ml - {variant.ConcentrationName} - {variant.Type}",
							ImageUrl = variant.Media?.FirstOrDefault(m => m.IsPrimary)?.Url ?? string.Empty,
							Quantity = quantity,
							Total = (int)itemTotal
						});
					}
				}

				decimal shippingFee = 0;
				if (!string.IsNullOrEmpty(request.WardCode) && request.DistrictId > 0)
				{
					// Use ShippingService to calculate shipping fee
					shippingFee = await _shippingService.CalculateShippingFeeAsync(request.DistrictId, request.WardCode) ?? 0;
				}

				decimal discount = 0;
				if (!string.IsNullOrEmpty(request.VoucherCode))
				{
					// Use VoucherService to calculate discount
					var voucherResponse = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode);
					if (voucherResponse == null)
					{
						return BaseResponse<PreviewOrderResponse>.Fail("Invalid voucher code.", ResponseErrorType.BadRequest);
					}
					var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherResponse.Id, subtotal);
					discount = subtotal - discountedTotal;
				}

				decimal total = subtotal + shippingFee - discount;

				var response = new PreviewOrderResponse
				{
					Items = items,
					SubTotal = subtotal,
					ShippingFee = shippingFee,
					Discount = discount,
					Total = total
				};

				return BaseResponse<PreviewOrderResponse>.Ok(response);
			}
			catch (Exception ex)
			{
				return BaseResponse<PreviewOrderResponse>.Fail($"An error occurred while previewing order: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Get the order
					var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// For offline orders, only allow cancellation
					if (order.Type == OrderType.Offline && request.Status != OrderStatus.Canceled)
					{
						return BaseResponse<string>.Fail(
							"Offline orders can only be cancelled. Other status updates are not allowed.",
							ResponseErrorType.BadRequest);
					}

					// Validate status transition
					var validationResult = _validationService.ValidateStatusTransition(order.Status, request.Status);
					if (!validationResult.Success)
					{
						return BaseResponse<string>.Fail(validationResult.Message ?? "Invalid status transition.", validationResult.ErrorType);
					}

					// Update order status
					order.Status = request.Status;
					order.StaffId = staffId;
					_unitOfWork.Orders.Update(order);

					// Update shipping status if applicable (only for online orders with shipping)
					if (order.ShippingInfo != null)
					{
						var shippingStatus = _shippingHelper.MapOrderStatusToShippingStatus(request.Status);
						if (shippingStatus.HasValue)
						{
							order.ShippingInfo.Status = shippingStatus.Value;
							_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
						}
					}

					// Create GHN shipping order when status is updated to Processing (for online orders with shipping)
					if (request.Status == OrderStatus.Processing && order.Type == OrderType.Online && order.ShippingInfo != null)
					{
						var recipientInfo = await _unitOfWork.RecipientInfos.GetByOrderIdAsync(order.Id);
						if (recipientInfo != null)
						{
							var ghnOrderResult = await _shippingHelper.CreateGHNShippingOrderAsync(order, recipientInfo);
							if (!ghnOrderResult.Success)
							{
								Console.WriteLine($"Warning: Failed to create GHN order: {ghnOrderResult.Message}");
							}
						}
					}

					// Handle order cancellation
					if (request.Status == OrderStatus.Canceled)
					{
						// Release reservations for online orders, restore inventory for offline orders
						if (order.Type == OrderType.Online)
						{
							var releaseResult = await _stockReservationService.ReleaseReservationAsync(order.Id);
							if (!releaseResult.Success)
							{
								return BaseResponse<string>.Fail(
									releaseResult.Message ?? "Failed to release stock reservation.",
									releaseResult.ErrorType);
							}
						}
						else
						{
							// For offline orders, restore inventory
							var itemsToRestore = order.OrderDetails
								.Select(od => (od.VariantId, od.Quantity))
								.ToList();

							var restoreResult = await _inventoryManager.RestoreInventoryAsync(itemsToRestore);
							if (!restoreResult.Success)
							{
								return BaseResponse<string>.Fail(
									restoreResult.Message ?? "Failed to restore inventory.",
									restoreResult.ErrorType);
							}
						}

						// Release voucher if it was used (only for online orders with customers)
						if (order.VoucherId.HasValue && order.CustomerId.HasValue)
						{
							var releaseVoucherResult = await _voucherService.ReleaseReservedVoucherAsync(
								order.CustomerId.Value,
								order.VoucherId.Value);

							// Log but don't fail if voucher release fails
							if (!releaseVoucherResult.Success)
							{
								// Consider logging this error
							}
						}
					}

					var orderTypeText = order.Type == OrderType.Online ? "online" : "in-store";
					return BaseResponse<string>.Ok($"Order status updated to {request.Status} for {orderTypeText} order.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred while updating order status: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Get the order
					var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// Verify the order belongs to the customer
					if (order.CustomerId != userId)
					{
						return BaseResponse<string>.Fail("You are not authorized to cancel this order.", ResponseErrorType.Forbidden);
					}

					// Only allow canceling online orders
					if (order.Type != OrderType.Online)
					{
						return BaseResponse<string>.Fail("Only online orders can be cancelled.", ResponseErrorType.BadRequest);
					}

					// Check if order can be cancelled (only Pending or Processing)
					if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
					{
						return BaseResponse<string>.Fail(
							$"Cannot cancel order with status {order.Status}. Only Pending or Processing orders can be cancelled.",
							ResponseErrorType.BadRequest);
					}

					// Check if order is already cancelled
					if (order.Status == OrderStatus.Canceled)
					{
						return BaseResponse<string>.Fail("Order is already cancelled.", ResponseErrorType.BadRequest);
					}

					// Update order status to Canceled
					order.Status = OrderStatus.Canceled;
					_unitOfWork.Orders.Update(order);

					// Update shipping status if applicable
					if (order.ShippingInfo != null)
					{
						order.ShippingInfo.Status = ShippingStatus.Cancelled;
						_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
					}

					// Restore inventory
					var itemsToRestore = order.OrderDetails
						.Select(od => (od.VariantId, od.Quantity))
						.ToList();

					var restoreResult = await _inventoryManager.RestoreInventoryAsync(itemsToRestore);
					if (!restoreResult.Success)
					{
						return BaseResponse<string>.Fail(
							restoreResult.Message ?? "Failed to restore inventory.",
							restoreResult.ErrorType);
					}

					// Release voucher if it was used
					if (order.VoucherId.HasValue && order.CustomerId.HasValue)
					{
						var releaseVoucherResult = await _voucherService.ReleaseReservedVoucherAsync(
							order.CustomerId.Value,
							order.VoucherId.Value);

						// Log but don't fail if voucher release fails
						if (!releaseVoucherResult.Success)
						{
							// Consider logging this error
						}
					}

					return BaseResponse<string>.Ok("Order has been cancelled successfully.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred while cancelling order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}
	}
}
