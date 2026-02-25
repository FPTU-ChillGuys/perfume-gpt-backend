using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class CartService : ICartService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly IShippingService _shippingService;
		private readonly IVoucherService _voucherService;
		private readonly IAddressService _addressService;

		public CartService(
			IUnitOfWork unitOfWork,
			IShippingService shippingService,
			IAddressService addressService,
			IVoucherService voucherService)
		{
			_unitOfWork = unitOfWork;
			_shippingService = shippingService;
			_addressService = addressService;
			_voucherService = voucherService;
		}

		#endregion Dependencies

		public async Task<BaseResponse<CartCheckoutResponse>> GetCartForCheckoutAsync(Guid userId, GetCartTotalRequest request)
		{
			try
			{
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<CartCheckoutResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
				}

				var items = await _unitOfWork.CartItems.GetCartCheckoutItemsAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<CartCheckoutResponse>.Ok(new CartCheckoutResponse
					{
						Items = [],
						ShippingFee = 0m,
						TotalPrice = 0m
					}, "Cart is empty");
				}

				var totalResult = await GetCartTotalAsync(userId, request);
				if (!totalResult.Success || totalResult.Payload == null)
				{
					return BaseResponse<CartCheckoutResponse>.Fail(totalResult.Message, totalResult.ErrorType);
				}

				var response = new CartCheckoutResponse
				{
					Items = items,
					ShippingFee = totalResult.Payload.ShippingFee,
					TotalPrice = totalResult.Payload.TotalPrice
				};

				return BaseResponse<CartCheckoutResponse>.Ok(response, "Cart retrieved for checkout");
			}
			catch (Exception ex)
			{
				return BaseResponse<CartCheckoutResponse>.Fail(
					$"Error retrieving cart for checkout: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId)
		{
			try
			{
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);

				var items = await _unitOfWork.CartItems.GetCartItemsByCartIdAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<GetCartItemsResponse>.Ok(new GetCartItemsResponse
					{
						Items = []
					}, "Cart is empty");
				}

				var response = new GetCartItemsResponse
				{
					Items = items
				};

				return BaseResponse<GetCartItemsResponse>.Ok(response, "Cart items retrieved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<GetCartItemsResponse>.Fail(
					$"Error retrieving cart items: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, GetCartTotalRequest request)
		{
			try
			{
				// 1. Get cart
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<GetCartTotalResponse>.Fail(
						"Cart not found",
						ResponseErrorType.NotFound);
				}

				// 2. Get cart items
				var items = await _unitOfWork.CartItems.GetCartItemPricesAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<GetCartTotalResponse>.Ok(
						new GetCartTotalResponse
						{
							Subtotal = 0m,
							ShippingFee = 0m,
							TotalPrice = 0m
						},
						"Cart is empty");
				}

				// 3. Calculate subtotal
				var subtotal = items.Sum(item => item.SubTotal);

				// 4. Calculate shipping fee if district and ward provided
				decimal shippingFee = 0m;
				if (request.DistrictId != null && request.DistrictId > 0 &&
					!string.IsNullOrWhiteSpace(request.WardCode))
				{
					var calculatedFee = await _shippingService.CalculateShippingFeeAsync(
						request.DistrictId.Value,
						request.WardCode);

					if (calculatedFee == null)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							"Failed to calculate shipping fee",
							ResponseErrorType.InternalError);
					}

					shippingFee = calculatedFee.Value;
				}

				// 5. Calculate total before discount
				var totalPrice = subtotal + shippingFee;

				// 6. Apply voucher discount if provided
				decimal discountAmount = 0m;
				if (!string.IsNullOrWhiteSpace(request.VoucherCode))
				{
					var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
						request.VoucherCode,
						userId);

					if (!voucherValidation.Success)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							voucherValidation.Message ?? "Voucher validation failed",
							voucherValidation.ErrorType);
					}

					var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(
						request.VoucherCode,
						totalPrice);

					discountAmount = totalPrice - discountedTotal;
					totalPrice = discountedTotal;
				}

				// 7. Build response
				var response = new GetCartTotalResponse
				{
					Subtotal = subtotal,
					ShippingFee = shippingFee,
					TotalPrice = totalPrice
				};

				return BaseResponse<GetCartTotalResponse>.Ok(
					response,
					"Cart total calculated successfully");
			}
			catch (Exception)
			{
				return BaseResponse<GetCartTotalResponse>.Fail(
					"An error occurred while calculating cart total",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId)
		{
			var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);

			try
			{
				var hasItems = await _unitOfWork.CartItems.HasItemsInCartAsync(cart.Id);
				if (hasItems)
				{
					var result = await _unitOfWork.Carts.ClearCartByUserIdAsync(userId);
					if (!result)
					{
						return BaseResponse<string>.Fail("Could not clear cart", ResponseErrorType.InternalError);
					}
				}
				return BaseResponse<string>.Ok("Cart cleared successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error clearing cart: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
