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

				// 4. Calculate shipping fee 
				decimal shippingFee = 0m;
				if (request.SavedAddressId != null)
				{
					var addressResult = await _addressService.GetAddressByIdAsync(userId, request.SavedAddressId.Value);
					if (!addressResult.Success || addressResult.Payload == null)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							"Saved address not found",
							ResponseErrorType.NotFound);
					}
					var calculatedFee = await _shippingService.CalculateShippingFeeAsync(
						addressResult.Payload.DistrictId,
						addressResult.Payload.WardCode);

					if (calculatedFee == null)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							"Failed to calculate shipping fee",
							ResponseErrorType.InternalError);
					}

					shippingFee = calculatedFee.Value;
				}
				else if (request.Recipient != null)
				{
					var calculatedFee = await _shippingService.CalculateShippingFeeAsync(
						request.Recipient.DistrictId,
						request.Recipient.WardCode);
					if (calculatedFee == null)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							"Failed to calculate shipping fee",
							ResponseErrorType.InternalError);
					}
					shippingFee = calculatedFee.Value;
				}

				// 5. Apply voucher discount if provided
				decimal finalAmount = subtotal;
				if (!string.IsNullOrWhiteSpace(request.VoucherCode))
				{

					var voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode);
					if (voucher == null)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							"Voucher not found",
							ResponseErrorType.NotFound);
					}

					var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
						request.VoucherCode,
						userId,
						subtotal,
						null); // Cart for signined-in users, not guests

					if (!voucherValidation.Success)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							voucherValidation.Message ?? "Voucher validation failed",
							voucherValidation.ErrorType);
					}

					finalAmount = await _voucherService.CalculateVoucherDiscountAsync(
						request.VoucherCode,
						subtotal);
				}

				// 6. Build response
				var response = new GetCartTotalResponse
				{
					Subtotal = subtotal,
					ShippingFee = shippingFee,
					Discount = subtotal - finalAmount,
					TotalPrice = finalAmount + shippingFee
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
