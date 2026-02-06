using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
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
	/// <summary>
	/// Core order service handling checkout, status updates, and cancellations.
	/// Fulfillment operations are delegated to IOrderFulfillmentService.
	/// </summary>
	public class OrderService : IOrderService
	{
		#region Dependencies

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
		private readonly IOrderFulfillmentService _fulfillmentService;
		private readonly IMapper _mapper;

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
			IStockReservationService stockReservationService,
			IOrderFulfillmentService fulfillmentService,
			IMapper mapper)
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
			_fulfillmentService = fulfillmentService;
			_mapper = mapper;
		}

		#endregion

		#region Query Operations

		/// <inheritdoc />
		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request)
		{
			try
			{
				var query = BuildOrderQuery(request);

				var totalCount = await query.CountAsync();
				var orders = await ApplyPagingAndSorting(query, request).ToListAsync();

				var orderListItems = _mapper.Map<List<OrderListItem>>(orders);
				var pagedResult = new PagedResult<OrderListItem>(
					orderListItems,
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

		/// <inheritdoc />
		public async Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId)
		{
			try
			{
			var order = await _unitOfWork.Orders.GetByConditionAsync(
					o => o.Id == orderId,
					o => o.Include(o => o.Customer)
						  .Include(o => o.Staff)
						  .Include(o => o.Voucher)
						  .Include(o => o.ShippingInfo)
						  .Include(o => o.RecipientInfo)
						  .Include(o => o.OrderDetails)
							  .ThenInclude(od => od.ProductVariant)
								  .ThenInclude(v => v.Media));

				if (order == null)
				{
					return BaseResponse<OrderResponse>.Fail("Order not found.", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<OrderResponse>(order);
				return BaseResponse<OrderResponse>.Ok(response, "Order retrieved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<OrderResponse>.Fail(
					$"An error occurred while retrieving order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		/// <inheritdoc />
		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByUserIdAsync(Guid userId, GetPagedOrdersRequest request)
		{
			try
			{
				var query = BuildOrderQuery(request)
					.Where(o => o.CustomerId == userId);

				var totalCount = await query.CountAsync();
				var orders = await ApplyPagingAndSorting(query, request).ToListAsync();

				var orderListItems = _mapper.Map<List<OrderListItem>>(orders);
				var pagedResult = new PagedResult<OrderListItem>(
					orderListItems,
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

		/// <inheritdoc />
		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByStaffIdAsync(Guid staffId, GetPagedOrdersRequest request)
		{
			try
			{
				var query = BuildOrderQuery(request)
					.Where(o => o.StaffId == staffId);

				var totalCount = await query.CountAsync();
				var orders = await ApplyPagingAndSorting(query, request).ToListAsync();

				var orderListItems = _mapper.Map<List<OrderListItem>>(orders);
				var pagedResult = new PagedResult<OrderListItem>(
					orderListItems,
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

		#endregion

		#region Checkout Operations

		/// <inheritdoc />
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

					// Get cart with items
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
					var order = CreateOnlineOrder(userId, request.VoucherId, cartResponse.Payload.TotalPrice, paymentExpiresAt, orderDetailsResult.Payload);
					await _unitOfWork.Orders.AddAsync(order);

					// Setup shipping if not pickup
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

		/// <inheritdoc />
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
					var (voucherId, discount) = await ResolveVoucherAsync(request.VoucherCode);

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

					if (voucherId.HasValue)
					{
						var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherId.Value, subtotal);
						discount = subtotal - discountedTotal;
						totalAmount = discountedTotal;
					}

					// Create order
					var order = CreateOfflineOrder(staffId, voucherId, totalAmount, orderDetailsResult.Payload);
					await _unitOfWork.Orders.AddAsync(order);

					// Setup shipping if needed
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

		/// <inheritdoc />
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

		#endregion

		#region Order Status Management

		/// <inheritdoc />
		public async Task<BaseResponse<PickListResponse>> UpdateOrderStatusAsync(
			Guid orderId,
			Guid staffId,
			UpdateOrderStatusRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var order = await _unitOfWork.Orders.GetByConditionAsync(
						o => o.Id == orderId,
						o => o.Include(o => o.ShippingInfo).Include(o => o.OrderDetails));

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

		/// <inheritdoc />
		public async Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
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

					// Update status
					order.Status = OrderStatus.Canceled;
					_unitOfWork.Orders.Update(order);

					// Update shipping status
					if (order.ShippingInfo != null)
					{
						order.ShippingInfo.Status = ShippingStatus.Cancelled;
						_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
					}

					// Release stock reservation
					var releaseResult = await _stockReservationService.ReleaseReservationAsync(order.Id);
					if (!releaseResult.Success)
					{
						return BaseResponse<string>.Fail(
							releaseResult.Message ?? "Failed to release stock reservation.",
							releaseResult.ErrorType);
					}

					// Release voucher
					await ReleaseVoucherIfUsedAsync(order);

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

		#endregion

		#region Fulfillment Operations (Delegated)

		/// <inheritdoc />
		public Task<BaseResponse<string>> FulfillOrderAsync(
			Guid orderId,
			Guid staffId,
			FulfillOrderRequest request)
		{
			return _fulfillmentService.FulfillOrderAsync(orderId, staffId, request);
		}

		/// <inheritdoc />
		public Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(
			Guid orderId,
			Guid staffId,
			SwapDamagedStockRequest request)
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

		private static Order CreateOnlineOrder(
			Guid customerId,
			Guid? voucherId,
			decimal totalAmount,
			DateTime paymentExpiresAt,
			List<OrderDetail> orderDetails)
		{
			return new Order
			{
				CustomerId = customerId,
				Type = OrderType.Online,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				VoucherId = voucherId,
				TotalAmount = totalAmount,
				PaymentExpiresAt = paymentExpiresAt,
				OrderDetails = orderDetails
			};
		}

		private static Order CreateOfflineOrder(
			Guid staffId,
			Guid? voucherId,
			decimal totalAmount,
			List<OrderDetail> orderDetails)
		{
			return new Order
			{
				CustomerId = null,
				StaffId = staffId,
				Type = OrderType.Offline,
				Status = OrderStatus.Pending,
				PaymentStatus = PaymentStatus.Unpaid,
				VoucherId = voucherId,
				TotalAmount = totalAmount,
				OrderDetails = orderDetails
			};
		}

		private async Task<(Guid? VoucherId, decimal Discount)> ResolveVoucherAsync(string? voucherCode)
		{
			if (string.IsNullOrEmpty(voucherCode))
			{
				return (null, 0m);
			}

			var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode);
			if (voucher == null)
			{
				throw new InvalidOperationException("Invalid voucher code.");
			}

			if (voucher.ExpiryDate < DateTime.UtcNow)
			{
				throw new InvalidOperationException("Voucher has expired.");
			}

			return (voucher.Id, 0m);
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

			var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherResponse.Id, subtotal);
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
			if (order.VoucherId.HasValue && order.CustomerId.HasValue)
			{
				await _voucherService.ReleaseReservedVoucherAsync(
					order.CustomerId.Value,
					order.VoucherId.Value);
			}
		}

		private IQueryable<Order> BuildOrderQuery(GetPagedOrdersRequest request)
		{
			IQueryable<Order> query = _unitOfWork.Orders.Query()
				.Include(o => o.Customer)
				.Include(o => o.Staff)
				.Include(o => o.ShippingInfo)
				.Include(o => o.OrderDetails);

			// Filter by status
			if (request.Status.HasValue)
			{
				query = query.Where(o => o.Status == request.Status.Value);
			}

			// Filter by type
			if (request.Type.HasValue)
			{
				query = query.Where(o => o.Type == request.Type.Value);
			}

			// Filter by payment status
			if (request.PaymentStatus.HasValue)
			{
				query = query.Where(o => o.PaymentStatus == request.PaymentStatus.Value);
			}

			// Filter by date range
			if (request.FromDate.HasValue)
			{
				query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
			}

			if (request.ToDate.HasValue)
			{
				query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
			}

			// Search by order ID or customer name
			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim().ToLower();
				query = query.Where(o =>
					o.Id.ToString().ToLower().Contains(searchTerm) ||
					(o.Customer != null && o.Customer.FullName != null && o.Customer.FullName.ToLower().Contains(searchTerm)));
			}

			return query;
		}

		private static IQueryable<Order> ApplyPagingAndSorting(IQueryable<Order> query, GetPagedOrdersRequest request)
		{
			// Apply sorting
			query = request.SortBy?.ToLower() switch
			{
				"createdat" => request.IsDescending
					? query.OrderByDescending(o => o.CreatedAt)
					: query.OrderBy(o => o.CreatedAt),
				"totalamount" => request.IsDescending
					? query.OrderByDescending(o => o.TotalAmount)
					: query.OrderBy(o => o.TotalAmount),
				"status" => request.IsDescending
					? query.OrderByDescending(o => o.Status)
					: query.OrderBy(o => o.Status),
				_ => query.OrderByDescending(o => o.CreatedAt) // Default: newest first
			};

			// Apply paging
			return query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize);
		}

		#endregion
	}
}
