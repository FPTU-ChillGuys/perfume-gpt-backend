using FluentValidation;
using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderDetails;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderService : IOrderService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAddressService _addressService;
		private readonly ICartService _cartService;
		private readonly IVariantService _variantService;
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;
		private readonly IVoucherService _voucherService;
		private readonly IShippingService _shippingService;
		private readonly IValidator<CreateOrderRequest> _createOrderValidator;
		private readonly IVnPayService _vnPayService;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public OrderService(
			IUnitOfWork unitOfWork,
			IAddressService addressService,
			ICartService cartService,
			IVariantService variantService,
			IStockService stockService,
			IBatchService batchService,
			IVoucherService voucherService,
			IShippingService shippingService,
			IValidator<CreateOrderRequest> createOrderValidator,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor)
		{
			_unitOfWork = unitOfWork;
			_addressService = addressService;
			_cartService = cartService;
			_variantService = variantService;
			_stockService = stockService;
			_batchService = batchService;
			_voucherService = voucherService;
			_shippingService = shippingService;
			_createOrderValidator = createOrderValidator;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<BaseResponse<string>> Checkout(CreateOrderRequest request)
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
							request.CustomerId);

						if (!voucherValidation.Success)
						{
							return BaseResponse<string>.Fail(
								voucherValidation.Message ?? "Voucher validation failed.",
								voucherValidation.ErrorType);
						}
					}

					var cartResponse = await _cartService.GetCartByUserIdAsync(request.CustomerId, request.VoucherId);
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

					var orderDetailsResult = await CreateOrderDetailsAsync(itemsToValidate);
					if (!orderDetailsResult.Success || orderDetailsResult.Payload == null)
					{
						return BaseResponse<string>.Fail(
							orderDetailsResult.Message ?? "Failed to create order details.",
							orderDetailsResult.ErrorType);
					}

					var order = new Order
					{
						CustomerId = request.CustomerId,
						StaffId = request.StaffId,
						Type = OrderType.Online,
						Status = OrderStatus.Pending,
						PaymentStatus = PaymentStatus.Unpaid,
						ExternalShopeeId = request.ExternalShopeeId,
						VoucherId = request.VoucherId,
						TotalAmount = cartResponse.Payload.TotalPrice,
						OrderDetails = orderDetailsResult.Payload
					};

					await _unitOfWork.Orders.AddAsync(order);
					// Don't save yet - let transaction handle it at the end

					if (!request.IsPickupInStore)
					{
						var shippingResult = await SetupShippingInfoAsync(
							order.Id,
							request.Recipient,
							request.CustomerId,
							cartResponse.Payload.ShippingFee);

						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}
					}

					var deductionResult = await DeductInventory(itemsToValidate);
					if (!deductionResult.Success)
					{
						return BaseResponse<string>.Fail(
							deductionResult.Message ?? "Inventory deduction failed.",
							deductionResult.ErrorType);
					}

					// Mark voucher as used if provided
					if (request.VoucherId.HasValue && request.VoucherId.Value != Guid.Empty)
					{
						var markVoucherResult = await _voucherService.MarkVoucherAsReservedAsync(
							request.CustomerId,
							request.VoucherId.Value);

						if (!markVoucherResult.Success)
						{
							return BaseResponse<string>.Fail(
								markVoucherResult.Message ?? "Failed to mark voucher as used.",
								markVoucherResult.ErrorType);
						}
					}

					return await CreatePaymentAndGenerateResponseAsync(
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

		public async Task<BaseResponse<string>> CheckoutInStore(CreateInStoreOrderRequest request)
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

					var orderDetailsResult = await CreateOrderDetailsAsync(itemsToValidate);
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
						StaffId = request.StaffId,
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
						var shippingResult = await SetupShippingInfoAsync(
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

					var deductionResult = await DeductInventory(itemsToValidate);
					if (!deductionResult.Success)
					{
						return BaseResponse<string>.Fail(
							deductionResult.Message ?? "Inventory deduction failed.",
							deductionResult.ErrorType);
					}

					// Note: We don't mark voucher as reserved for offline orders since there's no customer
					// The voucher discount is applied but not tracked per user

					return await CreatePaymentAndGenerateResponseAsync(
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
							ImageUrl = variant.ImageUrl ?? string.Empty,
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

		private async Task<BaseResponse<List<OrderDetail>>> CreateOrderDetailsAsync(List<(Guid VariantId, int Quantity)> items)
		{
			var stockValidation = await ValidateStockAvailability(items);
			if (!stockValidation.Success)
			{
				return BaseResponse<List<OrderDetail>>.Fail(
					stockValidation.Message ?? "Stock validation failed.",
					stockValidation.ErrorType);
			}

			var orderDetails = new List<OrderDetail>();
			foreach (var item in items)
			{
				var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
				if (!variantResponse.Success || variantResponse.Payload == null)
				{
					return BaseResponse<List<OrderDetail>>.Fail(
						$"Product variant {item.VariantId} not found.",
						ResponseErrorType.NotFound);
				}

				var variant = variantResponse.Payload;
				var orderDetail = new OrderDetail
				{
					VariantId = item.VariantId,
					Quantity = item.Quantity,
					UnitPrice = variant.BasePrice,
					Snapshot = $"{variant.ProductId} - {variant.VolumeMl}ml - {variant.ConcentrationName} - {variant.Type}"
				};

				orderDetails.Add(orderDetail);
			}

			return BaseResponse<List<OrderDetail>>.Ok(orderDetails);
		}

		private async Task<BaseResponse<decimal>> SetupShippingInfoAsync(
			Guid orderId,
			RecipientInformation? recipientRequest,
			Guid? customerId,
			decimal? preCalculatedShippingFee = null,
			Order? orderToUpdate = null)
		{
			RecipientInfo recipientInfo;

			// Resolve recipient information
			if (recipientRequest == null)
			{
				if (!customerId.HasValue)
				{
					return BaseResponse<decimal>.Fail(
						"Either recipient information or customer ID must be provided.",
						ResponseErrorType.BadRequest);
				}

				var customerAddress = await _addressService.GetDefaultAddressAsync(customerId.Value);
				if (customerAddress == null || !customerAddress.Success || customerAddress.Payload == null)
				{
					return BaseResponse<decimal>.Fail(
						"Customer default address not found. Please provide recipient information.",
						ResponseErrorType.BadRequest);
				}

				recipientInfo = new RecipientInfo
				{
					OrderId = orderId,
					FullName = customerAddress.Payload.ReceiverName,
					Phone = customerAddress.Payload.Phone,
					DistrictId = customerAddress.Payload.DistrictId,
					WardCode = customerAddress.Payload.WardCode,
					FullAddress = $"{customerAddress.Payload.Street}, {customerAddress.Payload.Ward}, {customerAddress.Payload.District}, {customerAddress.Payload.City}"
				};
			}
			else
			{
				recipientInfo = new RecipientInfo
				{
					OrderId = orderId,
					FullName = recipientRequest.FullName,
					Phone = recipientRequest.Phone,
					DistrictId = recipientRequest.DistrictId,
					WardCode = recipientRequest.WardCode,
					FullAddress = recipientRequest.FullAddress
				};
			}

			await _unitOfWork.RecipientInfos.AddAsync(recipientInfo);

			// Calculate or use pre-calculated shipping fee
			decimal shippingFee;
			if (preCalculatedShippingFee.HasValue)
			{
				shippingFee = preCalculatedShippingFee.Value;
			}
			else
			{
				// Calculate shipping fee using ShippingService
				var calculatedFee = await _shippingService.CalculateShippingFeeAsync(
					recipientInfo.DistrictId,
					recipientInfo.WardCode);

				if (calculatedFee == null)
				{
					return BaseResponse<decimal>.Fail("Failed to calculate shipping fee.", ResponseErrorType.InternalError);
				}

				shippingFee = calculatedFee.Value;
			}

			// Create shipping info
			var shippingInfo = new ShippingInfo
			{
				OrderId = orderId,
				CarrierName = CarrierName.GHN,
				TrackingNumber = null,
				ShippingFee = shippingFee,
				Status = ShippingStatus.Pending
			};

			await _unitOfWork.ShippingInfos.AddAsync(shippingInfo);

			// Update order total if order instance provided
			if (orderToUpdate != null)
			{
				var totalAmount = orderToUpdate.TotalAmount + shippingFee;
				orderToUpdate.TotalAmount = totalAmount;
				_unitOfWork.Orders.Update(orderToUpdate);
			}

			// Don't save - let transaction orchestrator handle it
			return BaseResponse<decimal>.Ok(shippingFee);
		}

		private async Task<BaseResponse<string>> CreatePaymentAndGenerateResponseAsync(
			Guid orderId,
			decimal amount,
			PaymentMethod paymentMethod,
			string successMessage)
		{
			var payment = new PaymentTransaction
			{
				OrderId = orderId,
				Method = paymentMethod,
				TransactionStatus = TransactionStatus.Pending,
				Amount = amount
			};

			await _unitOfWork.Payments.AddAsync(payment);
			// Don't save - let transaction orchestrator handle it

			if (paymentMethod == PaymentMethod.VnPay)
			{
				var httpContext = _httpContextAccessor.HttpContext;
				if (httpContext == null)
				{
					return BaseResponse<string>.Fail(
						"HttpContext is not available.",
						ResponseErrorType.InternalError);
				}

				var vnPayRequest = new VnPaymentRequest
				{
					OrderId = orderId,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
				return BaseResponse<string>.Ok(checkoutResponse.PaymentUrl, $"{successMessage} Please complete payment.");
			}
			else if (paymentMethod == PaymentMethod.Momo)
			{
				return BaseResponse<string>.Fail("Momo payment not yet implemented.", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(payment.Id.ToString(), successMessage);
		}

		private async Task<BaseResponse<bool>> ValidateStockAvailability(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				// Use StockService to validate stock
				var isStockValid = await _stockService.IsValidToCartAsync(item.VariantId, item.Quantity);
				if (!isStockValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient stock for {productName}.", ResponseErrorType.BadRequest);
				}

				// Use BatchService to validate batch availability
				var isBatchValid = await _batchService.ValidateBatchAvailabilityAsync(item.VariantId, item.Quantity);
				if (!isBatchValid)
				{
					var variantResponse = await _variantService.GetVariantByIdAsync(item.VariantId);
					var productName = variantResponse.Payload != null ? $"Variant {variantResponse.Payload.Sku}" : "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient batch quantity for {productName}.", ResponseErrorType.BadRequest);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		private async Task<BaseResponse<bool>> DeductInventory(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				// Use BatchService to deduct batches (FIFO)
				var batchDeducted = await _batchService.DeductBatchesByVariantAsync(item.VariantId, item.Quantity);
				if (!batchDeducted)
				{
					return BaseResponse<bool>.Fail($"Failed to deduct batch quantity for variant {item.VariantId}.", ResponseErrorType.InternalError);
				}

				// Use StockService to decrease stock
				var stockDecreased = await _stockService.DecreaseStockAsync(item.VariantId, item.Quantity);
				if (!stockDecreased)
				{
					return BaseResponse<bool>.Fail($"Failed to update stock for variant {item.VariantId}.", ResponseErrorType.InternalError);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}
	}
}
