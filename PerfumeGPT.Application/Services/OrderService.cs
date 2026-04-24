using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Extensions;
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
		private readonly INotificationService _notificationService;
		private readonly IGHNService _ghnService;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly INatsPublisherService _natsPublisher;
		private readonly ILogger<OrderService> _logger;

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
			IGHNService ghnService,
			IBackgroundJobService backgroundJobService,
			INatsPublisherService natsPublisher,
			ILogger<OrderService> logger)
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
			_ghnService = ghnService;
			_backgroundJobService = backgroundJobService;
			_natsPublisher = natsPublisher;
			_logger = logger;
		}
		#endregion Dependencies



		#region Query Operations
		public async Task<BaseResponse<OrderResponse>> GetOrderForPosPickupAsync(string orderCode)
		{
			if (string.IsNullOrWhiteSpace(orderCode))
			{
				return BaseResponse<OrderResponse>.Fail("Mã đơn hàng là bắt buộc.", ResponseErrorType.BadRequest);
			}

			var order = await _unitOfWork.Orders.GetOrderWithFullDetailsByCodeAsync(orderCode.Trim());
			if (order == null)
			{
				return BaseResponse<OrderResponse>.Fail("Không tìm thấy đơn hàng.", ResponseErrorType.NotFound);
			}

			var isStorePickupOrder = order.Type == OrderType.Online && order.ShippingInfo == null;
			if (!isStorePickupOrder)
			{
				return BaseResponse<OrderResponse>.Fail("Đây không phải đơn nhận tại cửa hàng.", ResponseErrorType.BadRequest);
			}

			if (order.Status != OrderStatus.ReadyToPick)
			{
				return BaseResponse<OrderResponse>.Fail($"Đơn hàng đang ở trạng thái {order.Status}. Vui lòng soạn hàng trước khi giao.", ResponseErrorType.BadRequest);
			}

			if (order.PaymentTransactions == null || order.PaymentTransactions.Count == 0 || order.PaymentTransactions[0].Id == Guid.Empty)
			{
				return BaseResponse<OrderResponse>.Fail("Đơn hàng thiếu thông tin thanh toán để xử lý tại quầy.", ResponseErrorType.BadRequest);
			}

			return BaseResponse<OrderResponse>.Ok(order, "Tra cứu đơn hàng thành công.");
		}

		public async Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request)
		{
			var (orders, totalCount) = await _unitOfWork.Orders.GetPagedOrdersAsync(request);

			var pagedResult = new PagedResult<OrderListItem>(
				orders,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<OrderListItem>>.Ok(pagedResult, "Lấy danh sách đơn hàng thành công.");
		}

		public async Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderWithFullDetailsAsync(orderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

			return BaseResponse<OrderResponse>.Ok(order, "Lấy thông tin đơn hàng thành công.");
		}

		public async Task<BaseResponse<ReceiptResponse>> GetInvoiceAsync(Guid orderId)
		{
			var invoice = await _unitOfWork.Orders.GetInvoiceAsync(orderId)
			 ?? throw AppException.NotFound("Không tìm thấy hóa đơn đơn hàng.");

			return BaseResponse<ReceiptResponse>.Ok(invoice, "Lấy hóa đơn đơn hàng thành công.");
		}
		#endregion Query Operations



		#region User Query Operations
		public async Task<BaseResponse<UserOrderResponse>> GetUserOrderByIdAsync(Guid orderId, Guid userId)
		{
			var order = await _unitOfWork.Orders.GetUserOrderWithFullDetailsAsync(orderId, userId);
			if (order == null)
			{
				return BaseResponse<UserOrderResponse>.Fail("Không tìm thấy đơn hàng hoặc đơn hàng không thuộc về người dùng.", ResponseErrorType.NotFound);
			}
			return BaseResponse<UserOrderResponse>.Ok(order, "Lấy thông tin đơn hàng thành công.");
		}

		public async Task<BaseResponse<ReceiptResponse>> GetMyInvoiceAsync(Guid orderId, Guid userId)
		{
			var invoice = await _unitOfWork.Orders.GetUserInvoiceAsync(orderId, userId);
			if (invoice == null)
			{
				return BaseResponse<ReceiptResponse>.Fail("Không tìm thấy hóa đơn hoặc hóa đơn không thuộc về người dùng.", ResponseErrorType.NotFound);
			}

			return BaseResponse<ReceiptResponse>.Ok(invoice, "Lấy hóa đơn đơn hàng thành công.");
		}
		#endregion User Query Operations



		#region Checkout Operations
		public async Task<BaseResponse<CreatePaymentResponseDto>> Checkout(Guid userId, CreateOrderRequest request)
		{
			var storePolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync()
				  ?? throw AppException.NotFound("Không tìm thấy cấu hình đặt cọc hệ thống.");

			var requiresDepositForThisOrder = (request.Payment.Method == PaymentMethod.CashOnDelivery || request.Payment.Method == PaymentMethod.CashInStore)
				&& storePolicy.IsDepositRequiredForCOD;

			if (requiresDepositForThisOrder)
			{
				if (!request.Payment.DepositGateway.HasValue)
					throw AppException.BadRequest("Đơn COD bắt buộc chọn cổng thanh toán đặt cọc.");

				if (request.Payment.DepositGateway != PaymentMethod.VnPay
					&& request.Payment.DepositGateway != PaymentMethod.Momo
					&& request.Payment.DepositGateway != PaymentMethod.PayOs)
				{
					throw AppException.BadRequest("Cổng thanh toán đặt cọc không hợp lệ. Chỉ hỗ trợ VNPay, Momo hoặc PayOs.");
				}
			}

			var customer = await _unitOfWork.Users.GetByIdAsync(userId) ??
			 throw AppException.NotFound("Không tìm thấy người dùng.");

			// Chặn luồng nếu vi phạm chính sách
			if ((request.Payment.Method == PaymentMethod.CashOnDelivery || request.Payment.Method == PaymentMethod.CashInStore) && !customer.IsEligibleForCod(DateTime.UtcNow))
			{
				throw AppException.BadRequest("Tài khoản của bạn đã vi phạm chính sách nhận hàng quá số lần quy định. Vui lòng chọn hình thức thanh toán trả trước (VNPay/MoMo) để tiếp tục mua sắm.");
			}
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
				  ?? throw AppException.BadRequest("Không thể lấy thông tin giỏ hàng để thanh toán.");

				if (cartResponse.Items.Count == 0)
					throw AppException.BadRequest("Giỏ hàng trống.");

				if (request.ExpectedTotalPrice.HasValue && Math.Abs(request.ExpectedTotalPrice.Value - cartResponse.TotalPrice) > 0.0001m)
					throw AppException.Conflict("Mức giảm giá không còn khớp. Vui lòng làm mới tổng tiền giỏ hàng và thanh toán lại.");

				// Create order details
				var itemsToValidate = cartResponse.Items
				.Select(item => (item.VariantId, item.Quantity))
				.ToList();

				// Create order and populate details through aggregate methods
				var order = Order.CreateOnline(
					userId,
					cartResponse.TotalPrice,
					storePolicy,
					requiresDepositForThisOrder);
				await _orderDetailsFactory.CreateOrderDetailsAsync(order, [.. cartResponse.Items]);

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
					throw AppException.BadRequest(latestTotalResponse.Message ?? "Không thể làm mới tổng tiền giỏ hàng.");

				if (Math.Abs(latestTotalResponse.Payload.TotalPrice - cartResponse.TotalPrice) > 0.0001m)
					throw AppException.Conflict("Mức giảm giá không còn khớp. Vui lòng làm mới tổng tiền giỏ hàng và thanh toán lại.");

				var promoUsageList = cartResponse.Items
					.Where(x => x.AppliedPromotionItemId.HasValue)
					.GroupBy(x => x.AppliedPromotionItemId!.Value)
					.Select(g => new { PromoId = g.Key, Qty = g.Sum(i => i.DiscountedQuantity) })
					.ToList();

				foreach (var usage in promoUsageList)
				{
					var promo = await _unitOfWork.PromotionItems.GetByIdAsync(usage.PromoId) ?? throw AppException.BadRequest("Không tìm thấy khuyến mãi cho sản phẩm đã áp dụng.");
					promo.IncreaseCurrentUsage(usage.Qty);
					_unitOfWork.PromotionItems.Update(promo);
				}

				await _unitOfWork.Orders.AddAsync(order);

				// Setup shipping if not pickup
				if (request.DeliveryMethod == DeliveryMethod.Delivery)
				{
					await _shippingHelper.SetupShippingInfoAsync(order, request.Recipient, userId, request.SavedAddressId, cartResponse.ShippingFee);
				}

				// Reserve stock
				await _stockReservationService.ReserveStockForOrderAsync(order.Id, itemsToValidate, order.PaymentExpiresAt);

				// Mark voucher as reserved
				if (voucher != null)
				{
					var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(userId, null, voucher.Id, order.Id)
						?? throw AppException.BadRequest("Không thể đánh dấu mã giảm giá đã sử dụng.");

					order.AssignVoucher(markVoucherResult);
				}

				// Clear cart Items
				await _cartService.ClearCartAsync(userId, request.ItemIds, false);

				// 1. GỌI HÀM CŨ ĐỂ TẠO GIAO DỊCH CỌC (Ví dụ: VNPay 100k) VÀ LẤY URL
				var paymentMethodForGateway = requiresDepositForThisOrder
					? request.Payment.DepositGateway!.Value
					: request.Payment.Method;

				var response = await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(order, paymentMethodForGateway, null);

				// ======================================================================
				// 2. ĐOẠN CODE BỔ SUNG: TẠO SẴN GIAO DỊCH COD CHO PHẦN CÒN LẠI (900k)
				// ======================================================================
				if (requiresDepositForThisOrder && (request.Payment.Method == PaymentMethod.CashOnDelivery || request.Payment.Method == PaymentMethod.CashInStore))
				{
					var remainingToCollect = order.TotalAmount - order.RequiredDepositAmount;
					if (remainingToCollect > 0)
					{
						// Khởi tạo giao dịch Pending cho số tiền còn lại
						var pendingRemainingTransaction = PaymentTransaction.Create(
							order.Id,
							request.Payment.Method, // CashOnDelivery hoặc CashInStore
							remainingToCollect);

						await _unitOfWork.Payments.AddAsync(pendingRemainingTransaction);
					}
				}
				// ======================================================================

				await _notificationService.SendToRoleAsync(
				UserRole.staff,
				"Đơn hàng online mới",
				$"Có đơn hàng Online #{order.Id} cần đóng gói. Tổng tiền: {cartResponse.TotalPrice:N0}.",
				NotificationType.Info,
				referenceId: order.Id,
				referenceType: NotifiReferecneType.Order);

				// Publish order_created event to Redis for AI backend (email notification)
				await _natsPublisher.PublishOrderCreatedAsync(order.Id, userId);

				return BaseResponse<CreatePaymentResponseDto>.Ok(response, "Thanh toán đơn hàng thành công.");
			});
		}

		public async Task<BaseResponse<CreatePaymentResponseDto>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request)
		{
			if (request.ScannedItems == null || request.ScannedItems.Count == 0)
				throw AppException.BadRequest("Không có sản phẩm trong đơn hàng.");

			// 1. KIỂM TRA CHÍNH SÁCH ĐẶT CỌC (BỔ SUNG MỚI)
			var storePolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync()
				  ?? throw AppException.NotFound("Không tìm thấy cấu hình đặt cọc hệ thống.");

			var requiresDepositForThisOrder = request.Payment.Method == PaymentMethod.CashOnDelivery
				&& storePolicy.IsDepositRequiredForCOD;

			if (requiresDepositForThisOrder)
			{
				if (!request.Payment.DepositGateway.HasValue)
					throw AppException.BadRequest("Đơn COD bắt buộc chọn cổng thanh toán để khách đặt cọc (VNPay/MoMo/PayOs).");

				if (request.Payment.DepositGateway != PaymentMethod.VnPay
					&& request.Payment.DepositGateway != PaymentMethod.Momo
					&& request.Payment.DepositGateway != PaymentMethod.PayOs)
				{
					throw AppException.BadRequest("Cổng thanh toán đặt cọc không hợp lệ.");
				}
			}

			// 2. KIỂM TRA ĐIỀU KIỆN KHÁCH HÀNG (Nếu là Member)
			if (request.Payment.Method == PaymentMethod.CashOnDelivery && request.CustomerId.HasValue)
			{
				var customer = await _unitOfWork.Users.GetByIdAsync(request.CustomerId.Value);
				if (customer != null && !customer.IsEligibleForCod(DateTime.UtcNow))
				{
					throw AppException.BadRequest("Tài khoản của khách hàng đã vi phạm chính sách nhận hàng. Vui lòng yêu cầu khách thanh toán trả trước toàn bộ.");
				}
			}

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
					var variantResponse = await _unitOfWork.Variants.GetByBarcodeAsync(scan.Barcode)
					  ?? throw AppException.NotFound($"Không tìm thấy biến thể với mã vạch {scan.Barcode}.");

					var variantId = variantResponse.Id;
					var variantBasePrice = variantResponse.BasePrice;

					var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b => b.BatchCode == scan.BatchCode && b.VariantId == variantId)
					 ?? throw AppException.NotFound($"Không tìm thấy lô {scan.BatchCode} cho sản phẩm {variantResponse.Sku}.");

					var subTotal = variantBasePrice * scan.Quantity;

					checkoutItems.Add(new CartCheckoutItemDto
					{
						VariantId = variantId,
						BatchId = batch.Id,
						VariantName = $"{variantResponse.Sku} - {variantResponse.VolumeMl}ml",
						Quantity = scan.Quantity,
						UnitPrice = variantBasePrice,
						SubTotal = subTotal,
						Discount = 0m,
						FinalTotal = subTotal,
						BatchCode = batch.BatchCode
					});
				}

				// 3. TÍNH TIỀN CHUNG (PRICING ENGINE)
				var (pricedItems, subtotal, finalAmount, voucherMessage) = await _cartService.CalculatePricingEngineAsync(
					checkoutItems,
					request.VoucherCode,
					request.CustomerId,
					request.GuestEmailOrPhoneNumber);

				if (request.ExpectedTotalPrice.HasValue && Math.Abs(request.ExpectedTotalPrice.Value - finalAmount) > 0.0001m)
					throw AppException.Conflict("Giá đã thay đổi so với lúc xem trước. Vui lòng làm mới và kiểm tra lại tổng tiền.");

				// 4. TẠO ĐƠN HÀNG CHỜ (PENDING) - BỔ SUNG THAM SỐ CỌC
				var order = Order.CreateOffline(
					request.CustomerId,
					request.GuestEmailOrPhoneNumber,
					staffId,
					finalAmount,
					storePolicy,
					request.Payment.Method == PaymentMethod.CashOnDelivery);

				await _orderDetailsFactory.CreateOrderDetailsAsync(order, pricedItems);

				// 5. CẬP NHẬT QUOTA PROMOTION
				var promoUsageList = pricedItems
					.Where(x => x.AppliedPromotionItemId.HasValue)
					.GroupBy(x => x.AppliedPromotionItemId!.Value)
					.Select(g => new { PromoId = g.Key, Qty = g.Sum(i => i.DiscountedQuantity) })
					.ToList();

				foreach (var usage in promoUsageList)
				{
					var promo = await _unitOfWork.PromotionItems.GetByIdAsync(usage.PromoId) ?? throw AppException.BadRequest("Không tìm thấy khuyến mãi cho sản phẩm đã áp dụng.");
					promo.IncreaseCurrentUsage(usage.Qty);
					_unitOfWork.PromotionItems.Update(promo);
				}

				await _unitOfWork.Orders.AddAsync(order);

				// 6. XỬ LÝ VOUCHER AN TOÀN
				if (!string.IsNullOrEmpty(request.VoucherCode))
				{
					var voucher = await ValidateAndGetVoucherAsync(
						request.VoucherCode,
						request.CustomerId,
						request.GuestEmailOrPhoneNumber,
						subtotal,
						pricedItems.Select(x => x.VariantId));

					if (voucher != null)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							request.CustomerId, request.GuestEmailOrPhoneNumber, voucher.Id, order.Id);

						if (markVoucherResult != null) order.AssignVoucher(markVoucherResult);
					}
				}

				// 7. GIAO HÀNG (Nếu không lấy tại cửa hàng)
				if (!request.IsPickupInStore && request.Recipient != null)
				{
					await _shippingHelper.SetupShippingInfoAsync(order, request.Recipient, request.CustomerId, null);
				}

				// 8. GIỮ KHO CHÍNH XÁC THEO LÔ 
				var posReservationItems = pricedItems
					.GroupBy(x => new { x.VariantId, x.BatchId })
					.Select(g => (VariantId: g.Key.VariantId, BatchId: g.Key.BatchId!.Value, Quantity: g.Sum(x => x.Quantity)))
					.ToList();

				await _stockReservationService.ReserveExactBatchStockForOrderAsync(order.Id, posReservationItems, order.PaymentExpiresAt);

				// 9. TẠO GIAO DỊCH THANH TOÁN (PENDING)
				// 9.1. GỌI HÀM CŨ ĐỂ TẠO GIAO DỊCH CỌC (Ví dụ: VNPay 100k) VÀ LẤY URL
				var paymentMethodForGateway = requiresDepositForThisOrder
					? request.Payment.DepositGateway!.Value
					: request.Payment.Method;

				var response = await _orderPaymentService.CreatePaymentAndGenerateResponseAsync(order, paymentMethodForGateway, request.PosSessionId);

				// ======================================================================
				// 9.2. ĐOẠN CODE BỔ SUNG: TẠO SẴN GIAO DỊCH COD CHO PHẦN CÒN LẠI (900k)
				// ======================================================================
				if (requiresDepositForThisOrder && (request.Payment.Method == PaymentMethod.CashOnDelivery || request.Payment.Method == PaymentMethod.CashInStore))
				{
					var remainingToCollect = order.TotalAmount - order.RequiredDepositAmount;
					if (remainingToCollect > 0)
					{
						// Khởi tạo giao dịch Pending cho số tiền còn lại
						var pendingRemainingTransaction = PaymentTransaction.Create(
							order.Id,
							request.Payment.Method, // CashOnDelivery hoặc CashInStore
							remainingToCollect);

						await _unitOfWork.Payments.AddAsync(pendingRemainingTransaction);
					}
				}
				// ======================================================================

				// Publish event
				if (request.CustomerId.HasValue)
					await _natsPublisher.PublishOrderCreatedAsync(order.Id, request.CustomerId.Value);

				return BaseResponse<CreatePaymentResponseDto>.Ok(response, "Tạo đơn hàng thành công. Đang chờ xác nhận thanh toán.");
			});
		}
		#endregion Checkout Operations



		#region Order Status Management
		public async Task<BaseResponse<PickListResponse?>> UpdateOrderStatusToPreparingAsync(Guid orderId, Guid staffId)
		{
			Guid? customerIdToNotify = null;

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (order.Status != OrderStatus.Pending)
					throw AppException.BadRequest($"Chỉ có thể cập nhật trạng thái đơn hàng từ Pending sang Preparing. Hiện tại: {order.Status}.");

				// ==========================================
				// 1. RÀO CHẮN TÀI CHÍNH BỌC THÉP (UPDATED)
				// ==========================================
				bool isFullyPaid = order.PaymentStatus == PaymentStatus.Paid;
				bool isDepositFulfilled = order.PaymentStatus == PaymentStatus.PartialPaid;

				bool isDelivery = order.ForwardShipping != null;
				bool isCod = order.PaymentTransactions.Any(p => p.Method == PaymentMethod.CashOnDelivery);
				bool isCashInStore = order.PaymentTransactions.Any(p => p.Method == PaymentMethod.CashInStore);

				bool canPrepare = false;

				if (order.RequiredDepositAmount > 0)
				{
					// NẾU ĐƠN CÓ YÊU CẦU CỌC: Bắt buộc phải thanh toán Full hoặc ít nhất đã trả đủ Cọc (PartiallyPaid)
					canPrepare = isFullyPaid || isDepositFulfilled;
				}
				else
				{
					// NẾU ĐƠN KHÔNG YÊU CẦU CỌC: Phải trả Full, HOẶC là COD, HOẶC là CashInStore đến tận nơi lấy
					canPrepare = isFullyPaid || isCod || (isCashInStore && !isDelivery);
				}

				if (!canPrepare)
				{
					string errorMessage = order.RequiredDepositAmount > 0
						? $"Đơn hàng này yêu cầu đặt cọc {order.RequiredDepositAmount:N0}đ. Khách hàng chưa hoàn tất thanh toán cọc."
						: "Không thể chuẩn bị đơn chưa thanh toán, trừ khi là COD hoặc thanh toán tại quầy cho đơn nhận tại cửa hàng.";

					throw AppException.BadRequest(errorMessage);
				}

				// ==========================================
				// 2. CHUYỂN TRẠNG THÁI VÀ SINH PICK LIST
				// ==========================================
				order.SetStatus(OrderStatus.Preparing);
				order.SetStaff(staffId);
				_unitOfWork.Orders.Update(order);
				customerIdToNotify = order.CustomerId;

				var pickListResponse = await _fulfillmentService.GetPickListAsync(order);

				var orderTypeText = order.Type == OrderType.Online ? "trực tuyến" : "giao tại cửa hàng";
				return BaseResponse<PickListResponse?>.Ok(pickListResponse, $"Đơn hàng đã sẵn sàng để soạn ({orderTypeText}).");
			});

			if (customerIdToNotify.HasValue)
			{
				await _notificationService.SendToUserAsync(
					customerIdToNotify.Value,
					"Đơn hàng đã được xác nhận",
					$"Đơn hàng #{orderId} của bạn đã được xác nhận và đang xử lý.",
					NotificationType.Info,
					referenceId: orderId,
					referenceType: NotifiReferecneType.Order);
			}

			return response;
		}

		public async Task<BaseResponse<string>> CancelOrderByStaffAsync(Guid orderId, Guid staffId, StaffCancelOrderRequest request)
		{
			Guid? createdCancelRequestId = null;
			Guid? customerIdToNotify = null;

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing && order.Status != OrderStatus.ReadyToPick)
					throw AppException.BadRequest($"Không thể hủy đơn ở trạng thái {order.Status}. Chỉ cho phép hủy đơn ở trạng thái Pending, Preparing hoặc ReadyToPick.");

				var isRefundRequired = order.PaidAmount > 0;

				// MAKER-CHECKER: Nếu đã thu tiền, nhân viên không được tự hoàn tiền, phải tạo Request
				if (isRefundRequired)
				{
					var hasPendingCancelRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(
						x => x.OrderId == order.Id && x.Status == CancelRequestStatus.Pending);

					if (hasPendingCancelRequest)
						throw AppException.BadRequest("Đơn hàng này đã có yêu cầu hủy đang chờ xử lý.");

					var payload = new CancelRequestPayload
					{
						Reason = request.Reason,
						IsRefundRequired = true,
						RefundAmount = order.PaidAmount, // Cửa hàng hủy nên trả 100% không phạt
						StaffNote = $"Cửa hàng chủ động hủy. Hoàn trả 100%. Lý do: {request.Note}"
					};

					var cancelRequest = OrderCancelRequest.Create(order.Id, staffId, payload);
					cancelRequest.Order = order;

					await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);
					createdCancelRequestId = cancelRequest.Id;
					return BaseResponse<string>.Ok("Đơn hàng đã thanh toán. Đã tạo yêu cầu hủy chờ Quản lý duyệt hoàn tiền.");
				}

				// NẾU CHƯA THU TIỀN: Hủy trực tiếp ngay lập tức
				await HandleOrderCancellationAsync(order, request.Reason);

				order.SetStaff(staffId);
				_unitOfWork.Orders.Update(order);
				customerIdToNotify = order.CustomerId;

				var orderType = order.Type == OrderType.Online ? "trực tuyến" : "tại cửa hàng";
				return BaseResponse<string>.Ok($"Hủy đơn hàng {orderType} thành công.");
			});

			if (createdCancelRequestId.HasValue)
			{
				await _notificationService.SendToRoleAsync(
					UserRole.admin,
					"Yêu cầu duyệt hoàn tiền (Từ nhân viên)",
					$"Nhân viên yêu cầu duyệt hủy và hoàn tiền cho đơn #{orderId}.",
					NotificationType.Warning,
					referenceId: createdCancelRequestId,
					referenceType: NotifiReferecneType.OrderCancelRequest);
			}

			if (customerIdToNotify.HasValue)
			{
				await _notificationService.SendToUserAsync(
					customerIdToNotify.Value,
					"Đơn hàng đã bị hủy",
					$"Đơn hàng #{orderId} của bạn đã bị hủy từ hệ thống. Lý do: {request.Reason}.",
					NotificationType.Warning,
					referenceId: orderId,
					referenceType: NotifiReferecneType.Order);
			}

			return response;
		}

		public async Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId, UserCancelOrderRequest request)
		{
			Guid? createdCancelRequestId = null;

			var response = await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForCancellationAsync(orderId)
					?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				// Validate authorization
				order.EnsureOwnedBy(userId);

				// Validate order type
				order.EnsureOnlineOrder();

				if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Preparing && order.Status != OrderStatus.ReadyToPick)
					throw AppException.BadRequest($"Không thể hủy đơn ở trạng thái {order.Status}. Chỉ cho phép hủy đơn ở trạng thái Pending, Preparing hoặc ReadyToPick.");

				// 💥 ĐÃ SỬA: Tận dụng Snapshot để lấy mức phạt chuẩn xác (O(1) - Không cần query DB)
				var penaltyAmount = 0m;
				if (order.Status == OrderStatus.Preparing || order.Status == OrderStatus.ReadyToPick)
				{
					penaltyAmount = order.PolicyDepositAmount; // Phạt bằng đúng mức cọc quy định lúc tạo đơn
				}

				// Số tiền thực nhận lại = Tổng tiền đã trả - Tiền phạt
				var actualRefundAmount = Math.Max(0, order.PaidAmount - penaltyAmount);
				var isRefundRequired = actualRefundAmount > 0;

				// Pending + Unpaid => auto cancel immediately
				if (order.Status == OrderStatus.Pending && !isRefundRequired)
				{
					await HandleOrderCancellationAsync(order, request.Reason);
					_unitOfWork.Orders.Update(order);

					return BaseResponse<string>.Ok("Hủy đơn hàng thành công.");
				}

				var hasPendingCancelRequest = await _unitOfWork.OrderCancelRequests.AnyAsync(
					x => x.OrderId == order.Id && x.Status == CancelRequestStatus.Pending);

				if (hasPendingCancelRequest)
					throw AppException.BadRequest("Đơn hàng này đã có yêu cầu hủy đang chờ xử lý.");

				var payload = new CancelRequestPayload
				{
					Reason = request.Reason,
					IsRefundRequired = isRefundRequired,
					RefundAmount = isRefundRequired ? actualRefundAmount : null,
					StaffNote = penaltyAmount > 0 ? $"Khách hàng hủy khi đơn đang xử lý. Áp dụng phạt (tương đương cọc): {penaltyAmount:N0}đ." : null,
					RefundBankName = request.RefundBankName,
					RefundAccountNumber = request.RefundAccountNumber,
					RefundAccountName = request.RefundAccountName
				};

				var cancelRequest = OrderCancelRequest.Create(order.Id, userId, payload);
				cancelRequest.Order = order;

				await _unitOfWork.OrderCancelRequests.AddAsync(cancelRequest);
				createdCancelRequestId = cancelRequest.Id;

				return BaseResponse<string>.Ok("Gửi yêu cầu hủy đơn thành công.");
			});

			if (createdCancelRequestId.HasValue)
			{
				await _notificationService.SendToRoleAsync(
					UserRole.admin,
					"Yêu cầu hủy đơn mới",
					$"Khách hàng yêu cầu hủy đơn #{orderId}.",
					NotificationType.Warning,
					referenceId: createdCancelRequestId,
					referenceType: NotifiReferecneType.OrderCancelRequest);

				await _notificationService.SendToRoleAsync(
					UserRole.staff,
					"Yêu cầu hủy đơn mới",
					$"Khách hàng yêu cầu hủy đơn #{orderId}.",
					NotificationType.Warning,
					referenceId: createdCancelRequestId,
					referenceType: NotifiReferecneType.OrderCancelRequest);
			}

			return response;
		}
		#endregion Order Status Management



		#region Fulfillment Operations
		public async Task<BaseResponse<PickListResponse>> GetOrderPickListAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderForPickListAsync(orderId)
			 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");
			return BaseResponse<PickListResponse>.Ok(await _fulfillmentService.GetPickListAsync(order));
		}

		public async Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			return BaseResponse<string>.Ok(await _fulfillmentService.FulfillOrderAsync(orderId, staffId, request));
		}

		public async Task<BaseResponse<string>> DeliverOrderToInStoreCustomerAsync(Guid orderId, Guid staffId)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(orderId)
				 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

				if (order.Type != OrderType.Online)
					throw AppException.BadRequest("Thao tác này chỉ hỗ trợ đơn hàng trực tuyến.");

				if (order.Status != OrderStatus.ReadyToPick)
					throw AppException.BadRequest($"Đơn hàng phải ở trạng thái ReadyToPick. Hiện tại: {order.Status}.");

				if (order.ForwardShipping != null)
					throw AppException.BadRequest("Đơn hàng này được cấu hình giao tận nơi. Vui lòng dùng quy trình giao hàng để hoàn tất.");

				// 1. CẬP NHẬT TRẠNG THÁI VÀ NHÂN VIÊN GIAO HÀNG
				order.SetStatus(OrderStatus.Delivered);
				order.SetStaff(staffId);

				// 2. XỬ LÝ DÒNG TIỀN (NẾU KHÁCH CHỌN TRẢ SAU/COD)
				if (order.PaymentStatus != PaymentStatus.Paid && order.RemainingAmount > 0)
				{
					// SỬA LỖI 3: Chỉ ghi nhận đúng số tiền còn thiếu
					order.RecordPayment(order.RemainingAmount, DateTime.UtcNow);
				}

				var pendingTx = order.PaymentTransactions.FirstOrDefault(t =>
					t.TransactionStatus == TransactionStatus.Pending &&
					(t.Method == PaymentMethod.CashInStore || t.Method == PaymentMethod.CashOnDelivery));

				if (pendingTx != null)
				{
					pendingTx.MarkSuccess("Khách hàng đã thanh toán phần còn lại và nhận hàng tại cửa hàng.");
					_unitOfWork.Payments.Update(pendingTx);
				}

				// 3. Schedule loyalty points after return window (Delivered + 10 days)
				if (order.CustomerId.HasValue)
				{
					int points = (int)(order.TotalAmount * 0.01m);
					if (points > 0)
					{
						_backgroundJobService.ScheduleLoyaltyPointsGrant(_logger, order.Id, DateTime.UtcNow);
					}
				}

				_unitOfWork.Orders.Update(order);

				return BaseResponse<string>.Ok(order.Code);
			});
		}

		public async Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request)
		{
			return BaseResponse<SwapDamagedStockResponse>.Ok(await _fulfillmentService.SwapDamagedStockAsync(orderId, staffId, request));
		}
		#endregion Fulfillment Operations



		#region Private Helper Methods
		private async Task HandleOrderCancellationAsync(Order order, CancelOrderReason cancelReason)
		{
			var orderWithDetails = order.OrderDetails.Count > 0
				  ? order
				  : await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(order.Id)
					 ?? throw AppException.NotFound("Không tìm thấy đơn hàng.");

			var promoUsageList = orderWithDetails.OrderDetails
				.Where(x => x.PromotionItemId.HasValue)
				.GroupBy(x => x.PromotionItemId!.Value)
				.Select(g => new { PromoId = g.Key, Qty = g.Sum(i => i.Quantity) })
				.ToList();

			foreach (var usage in promoUsageList)
			{
				var promo = await _unitOfWork.PromotionItems.GetByIdAsync(usage.PromoId)
					?? throw AppException.BadRequest("Không tìm thấy khuyến mãi cho sản phẩm đã áp dụng.");

				promo.DecreaseCurrentUsage(usage.Qty);
				_unitOfWork.PromotionItems.Update(promo);
			}

			if (!string.IsNullOrWhiteSpace(order.ForwardShipping?.TrackingNumber))
			{
				await _ghnService.CancelOrderAsync(new CancelOrderRequest
				{
					TrackingNumbers = [order.ForwardShipping.TrackingNumber]
				});
			}

			if (order.Status == OrderStatus.ReadyToPick && orderWithDetails.PaymentTransactions.Any(p => p.Method == PaymentMethod.CashInStore))
			{
				order.CancelCashInStore(cancelReason);
			}
			else
			{
				order.SetStatus(OrderStatus.Cancelled);
			}

			foreach (var payment in orderWithDetails.PaymentTransactions.Where(p => p.IsPending()))
			{
				payment.MarkCancelled("Đơn hàng đã bị hủy.");
				_unitOfWork.Payments.Update(payment);
			}

			// Gọi hàm mới nâng cấp
			await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(order.Id);

			await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);
		}

		private async Task<VoucherResponse> ValidateAndGetVoucherAsync(string voucherCode, Guid? userId, string? phoneNumber, decimal totalPrice, IEnumerable<Guid>? cartVariantIds = null)
		{
			// Get voucher details
			var voucher = await _unitOfWork.Vouchers.GetByCodeAsync(voucherCode)
			   ?? throw AppException.NotFound("Không tìm thấy mã giảm giá.");

			// Validate voucher eligibility
			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(voucherCode, userId, totalPrice, phoneNumber, cartVariantIds);
			if (!voucherValidation) throw AppException.BadRequest("Xác thực mã giảm giá thất bại.");

			return voucher;
		}
		#endregion Private Helper Methods
	}
}

