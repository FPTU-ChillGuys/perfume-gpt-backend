using FluentValidation;
using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderDetails;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories;
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
		private readonly IValidator<CreateOrderRequest> _createOrderValidator;
		private readonly IVnPayService _vnPayService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly ICartService _cartService;
		private readonly IVariantRepository _variantRepository;
		private readonly IStockRepository _stockRepository;
		private readonly IBatchRepository _batchRepository;
		private readonly IGHNService _ghnService;
		private readonly IVoucherRepository _voucherRepository;
		private readonly IRecipientInfoRepository _recipientInfoRepository;
		private readonly IShippingInfoRepository _shippingInfoRepository;

		public OrderService(
			IUnitOfWork unitOfWork,
			IValidator<CreateOrderRequest> createOrderValidator,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor,
			IVariantRepository variantRepository,
			ICartService cartService,
			IVoucherRepository voucherRepository,
			IStockRepository stockRepository,
			IBatchRepository batchRepository,
			IGHNService ghnService,
			IRecipientInfoRepository recipientInfoRepository,
			IShippingInfoRepository shippingInfoRepository,
			IAddressService addressService)
		{
			_unitOfWork = unitOfWork;
			_createOrderValidator = createOrderValidator;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
			_variantRepository = variantRepository;
			_cartService = cartService;
			_stockRepository = stockRepository;
			_batchRepository = batchRepository;
			_voucherRepository = voucherRepository;
			_ghnService = ghnService;
			_recipientInfoRepository = recipientInfoRepository;
			_shippingInfoRepository = shippingInfoRepository;
			_addressService = addressService;
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
					await _unitOfWork.SaveChangesAsync();

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

					decimal totalAmount = orderDetailsResult.Payload.Sum(od => od.UnitPrice * od.Quantity);

					var order = new Order
					{
						CustomerId = null,
						StaffId = request.StaffId,
						Type = OrderType.Offline,
						Status = OrderStatus.Pending,
						PaymentStatus = PaymentStatus.Unpaid,
						TotalAmount = totalAmount,
						OrderDetails = orderDetailsResult.Payload
					};

					await _unitOfWork.Orders.AddAsync(order);
					await _unitOfWork.SaveChangesAsync();

					if (!request.IsPickupInStore && request.Recipient != null)
					{
						var shippingResult = await SetupShippingInfoWithFeeCalculationAsync(order, request.Recipient);
						if (!shippingResult.Success)
						{
							return BaseResponse<string>.Fail(
								shippingResult.Message ?? "Failed to setup shipping info.",
								shippingResult.ErrorType);
						}

						totalAmount = shippingResult.Payload;
					}

					var deductionResult = await DeductInventory(itemsToValidate);
					if (!deductionResult.Success)
					{
						return BaseResponse<string>.Fail(
							deductionResult.Message ?? "Inventory deduction failed.",
							deductionResult.ErrorType);
					}

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
					var variant = await _variantRepository.GetByBarcodeAsync(barcode);
					if (variant == null)
					{
						return BaseResponse<PreviewOrderResponse>.Fail($"Product with barcode {barcode} not found.", ResponseErrorType.NotFound);
					}

					var quantity = request.BarCodes.Count(b => b == barcode);
					var itemTotal = variant.BasePrice * quantity;
					subtotal += itemTotal;

					if (!items.Any(i => i.VariantId == variant.Id))
					{
						items.Add(new OrderDetailListItems
						{
							VariantId = variant.Id,
							VariantName = $"{variant.Product?.Name ?? "Unknown"} - {variant.VolumeMl}ml - {variant.Concentration?.Name ?? "Unknown"} - {variant.Type}",
							ImageUrl = variant.ImageUrl ?? string.Empty,
							Quantity = quantity,
							Total = (int)itemTotal
						});
					}
				}

				decimal shippingFee = 0;
				if (!string.IsNullOrEmpty(request.WardCode) && request.DistrictId > 0)
				{
					var calculateFeeRequest = new CalculateFeeRequest
					{
						ToWardCode = request.WardCode,
						ToDistrictId = request.DistrictId
					};

					var feeResponse = await _ghnService.CalculateShippingFeeAsync(calculateFeeRequest);
					if (feeResponse != null && feeResponse.Code == 200)
					{
						shippingFee = feeResponse.Data?.Total ?? 0;
					}
				}

				decimal discount = await CalculateDiscount(subtotal, request.VoucherId);
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
				var variant = await _variantRepository.GetByIdAsync(item.VariantId);
				if (variant == null)
				{
					return BaseResponse<List<OrderDetail>>.Fail(
						$"Product variant {item.VariantId} not found.",
						ResponseErrorType.NotFound);
				}

				var orderDetail = new OrderDetail
				{
					VariantId = item.VariantId,
					Quantity = item.Quantity,
					UnitPrice = variant.BasePrice,
					Snapshot = $"{variant.Product?.Name ?? "Unknown"} - {variant.VolumeMl}ml - {variant.Concentration?.Name ?? "Unknown"} - {variant.Type}"
				};

				orderDetails.Add(orderDetail);
			}

			return BaseResponse<List<OrderDetail>>.Ok(orderDetails);
		}

		private async Task<BaseResponse<bool>> SetupShippingInfoAsync(
			Guid orderId,
			RecipientInformation? recipientRequest,
			Guid customerId,
			decimal shippingFee)
		{
			RecipientInfo recipientInfo;

			if (recipientRequest == null)
			{
				var customerAddress = await _addressService.GetDefaultAddressAsync(customerId);
				if (customerAddress == null || !customerAddress.Success || customerAddress.Payload == null)
				{
					return BaseResponse<bool>.Fail(
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

			await _recipientInfoRepository.AddAsync(recipientInfo);

			var shippingInfo = new ShippingInfo
			{
				OrderId = orderId,
				CarrierName = CarrierName.GHN,
				TrackingNumber = null,
				ShippingFee = shippingFee,
				Status = ShippingStatus.Pending
			};

			await _shippingInfoRepository.AddAsync(shippingInfo);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}

		private async Task<BaseResponse<decimal>> SetupShippingInfoWithFeeCalculationAsync(
			Order order,
			RecipientInformation recipientRequest)
		{
			var recipientInfo = new RecipientInfo
			{
				OrderId = order.Id,
				FullName = recipientRequest.FullName,
				Phone = recipientRequest.Phone,
				DistrictId = recipientRequest.DistrictId,
				WardCode = recipientRequest.WardCode,
				FullAddress = recipientRequest.FullAddress
			};

			await _recipientInfoRepository.AddAsync(recipientInfo);

			var calculateFeeRequest = new CalculateFeeRequest
			{
				ToWardCode = recipientRequest.WardCode,
				ToDistrictId = recipientRequest.DistrictId
			};

			var feeResponse = await _ghnService.CalculateShippingFeeAsync(calculateFeeRequest);
			if (feeResponse == null || feeResponse.Code != 200)
			{
				return BaseResponse<decimal>.Fail("Failed to calculate shipping fee.", ResponseErrorType.InternalError);
			}

			var shippingInfo = new ShippingInfo
			{
				OrderId = order.Id,
				CarrierName = CarrierName.GHN,
				TrackingNumber = null,
				ShippingFee = feeResponse.Data.Total,
				Status = ShippingStatus.Pending
			};

			await _shippingInfoRepository.AddAsync(shippingInfo);
			await _unitOfWork.SaveChangesAsync();

			var totalAmount = order.TotalAmount + feeResponse.Data.Total;
			order.TotalAmount = totalAmount;
			_unitOfWork.Orders.Update(order);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<decimal>.Ok(totalAmount);
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
			await _unitOfWork.SaveChangesAsync();

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
				var isStockValid = await _stockRepository.IsValidToCart(item.VariantId, item.Quantity);
				if (!isStockValid)
				{
					var variant = await _variantRepository.GetByIdAsync(item.VariantId);
					var productName = variant?.Product?.Name ?? "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient stock for {productName}.", ResponseErrorType.BadRequest);
				}

				var isBatchValid = await _batchRepository.IsValidForDeductionAsync(item.VariantId, item.Quantity);
				if (!isBatchValid)
				{
					var variant = await _variantRepository.GetByIdAsync(item.VariantId);
					var productName = variant?.Product?.Name ?? "Unknown product";
					return BaseResponse<bool>.Fail($"Insufficient batch quantity for {productName}.", ResponseErrorType.BadRequest);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		private async Task<BaseResponse<bool>> DeductInventory(List<(Guid VariantId, int Quantity)> items)
		{
			foreach (var item in items)
			{
				var batchDeducted = await _batchRepository.DeductBathAsync(item.VariantId, item.Quantity);
				if (!batchDeducted)
				{
					return BaseResponse<bool>.Fail($"Failed to deduct batch quantity for variant {item.VariantId}.", ResponseErrorType.InternalError);
				}

				var stockUpdated = await _stockRepository.UpdateStockAsync(item.VariantId);
				if (!stockUpdated)
				{
					return BaseResponse<bool>.Fail($"Failed to update stock for variant {item.VariantId}.", ResponseErrorType.InternalError);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		private async Task<decimal> CalculateDiscount(decimal subtotal, Guid? voucherId)
		{
			if (!voucherId.HasValue || voucherId.Value == Guid.Empty)
			{
				return 0;
			}

			var voucher = await _voucherRepository.GetByIdAsync(voucherId.Value);
			if (voucher == null || voucher.ExpiryDate < DateTime.UtcNow || subtotal < voucher.MinOrderValue)
			{
				return 0;
			}

			if (voucher.DiscountType == DiscountType.Percentage)
			{
				return subtotal * (voucher.DiscountValue / 100);
			}
			else
			{
				return voucher.DiscountValue;
			}
		}
	}
}
