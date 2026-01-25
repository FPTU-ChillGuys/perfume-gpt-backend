using MapsterMapper;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class CartService : ICartService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IShippingService _shippingService;
		private readonly IVoucherService _voucherService;
		private readonly IAddressService _addressService;
		private readonly IMapper _mapper;

		public CartService(
			IUnitOfWork unitOfWork,
			IShippingService shippingService,
			IAddressService addressService,
			IVoucherService voucherService,
			IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_shippingService = shippingService;
			_addressService = addressService;
			_voucherService = voucherService;
			_mapper = mapper;
		}

		public async Task<BaseResponse<GetCartResponse>> GetCartByUserIdAsync(Guid userId, Guid? voucherId)
		{
			try
			{
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<GetCartResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
				}

				var items = await _unitOfWork.CartItems.GetCartItemByCartIdAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<GetCartResponse>.Ok(new GetCartResponse
					{
						Items = [],
						ShippingFee = 0m,
						TotalPrice = 0m
					}, "Cart is empty");
				}

				var itemResponses = _mapper.Map<List<GetCartItemResponse>>(items);

				var addressResponse = await _addressService.GetDefaultAddressAsync(userId);
				if (!addressResponse.Success || addressResponse.Payload == null)
				{
					return BaseResponse<GetCartResponse>.Fail(
						"Default address not found. Please set a default address to calculate shipping.",
						ResponseErrorType.NotFound);
				}

				// Use ShippingService to calculate shipping fee
				var shippingFee = await _shippingService.CalculateShippingFeeAsync(
					addressResponse.Payload.DistrictId,
					addressResponse.Payload.WardCode);

				if (shippingFee == null)
				{
					return BaseResponse<GetCartResponse>.Fail(
						"Failed to calculate shipping fee",
						ResponseErrorType.InternalError);
				}

				var subtotal = CalculateSubtotal(itemResponses);
				var totalPrice = subtotal + shippingFee.Value;

				if (voucherId.HasValue)
				{
					// Validate voucher before applying discount
					var voucherValidation = await _voucherService.ValidateToApplyVoucherAsync(voucherId.Value, userId);
					if (!voucherValidation.Success)
					{
						return BaseResponse<GetCartResponse>.Fail(
							voucherValidation.Message,
							voucherValidation.ErrorType);
					}

					// Use VoucherService to calculate discount
					totalPrice = await _voucherService.CalculateVoucherDiscountAsync(voucherId.Value, totalPrice);
					totalPrice = Math.Round(totalPrice, 0, MidpointRounding.AwayFromZero);
				}

				var response = new GetCartResponse
				{
					Items = itemResponses,
					ShippingFee = shippingFee.Value,
					TotalPrice = totalPrice
				};

				return BaseResponse<GetCartResponse>.Ok(response, "Cart retrieved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<GetCartResponse>.Fail(
					$"Error retrieving cart: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId)
		{
			try
			{
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<GetCartItemsResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
				}

				var items = await _unitOfWork.CartItems.GetCartItemByCartIdAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<GetCartItemsResponse>.Ok(new GetCartItemsResponse
					{
						Items = []
					}, "Cart is empty");
				}

				var itemResponses = _mapper.Map<List<GetCartItemResponse>>(items);

				var response = new GetCartItemsResponse
				{
					Items = itemResponses
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

		public async Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, Guid? voucherId)
		{
			try
			{
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<GetCartTotalResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
				}

				var items = await _unitOfWork.CartItems.GetCartItemByCartIdAsync(cart.Id);
				if (items == null || items.Count == 0)
				{
					return BaseResponse<GetCartTotalResponse>.Ok(new GetCartTotalResponse
					{
						Subtotal = 0m,
						ShippingFee = 0m,
						Discount = 0m,
						TotalPrice = 0m
					}, "Cart is empty");
				}

				var itemResponses = _mapper.Map<List<GetCartItemResponse>>(items);

				var addressResponse = await _addressService.GetDefaultAddressAsync(userId);
				if (!addressResponse.Success || addressResponse.Payload == null)
				{
					return BaseResponse<GetCartTotalResponse>.Fail(
						"Default address not found. Please set a default address to calculate shipping.",
						ResponseErrorType.NotFound);
				}

				// Use ShippingService to calculate shipping fee
				var shippingFee = await _shippingService.CalculateShippingFeeAsync(
					addressResponse.Payload.DistrictId,
					addressResponse.Payload.WardCode);

				if (shippingFee == null)
				{
					return BaseResponse<GetCartTotalResponse>.Fail(
						"Failed to calculate shipping fee",
						ResponseErrorType.InternalError);
				}

				var subtotal = CalculateSubtotal(itemResponses);
				var totalPrice = subtotal + shippingFee.Value;

				if (voucherId.HasValue)
				{
					// Validate voucher before applying discount
					var voucherValidation = await _voucherService.ValidateToApplyVoucherAsync(voucherId.Value, userId);
					if (!voucherValidation.Success)
					{
						return BaseResponse<GetCartTotalResponse>.Fail(
							voucherValidation.Message,
							voucherValidation.ErrorType);
					}

					// Use VoucherService to calculate discount
					totalPrice = await _voucherService.CalculateVoucherDiscountAsync(voucherId.Value, totalPrice);
					totalPrice = Math.Round(totalPrice, 0, MidpointRounding.AwayFromZero);
				}

				var response = new GetCartTotalResponse
				{
					Subtotal = subtotal,
					ShippingFee = shippingFee.Value,
					Discount = subtotal + shippingFee.Value - totalPrice,
					TotalPrice = totalPrice
				};

				return BaseResponse<GetCartTotalResponse>.Ok(response, "Cart total calculated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<GetCartTotalResponse>.Fail(
					$"Error calculating cart total: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		private static decimal CalculateSubtotal(List<GetCartItemResponse> items)
		{
			return items.Sum(item => item.SubTotal);
		}

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId)
		{
			var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
			if (cart == null)
			{
				return BaseResponse<string>.Fail("Cart not found", ResponseErrorType.NotFound);
			}
			try
			{
				var items = await _unitOfWork.CartItems.GetCartItemByCartIdAsync(cart.Id);
				if (items != null && items.Count > 0)
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
