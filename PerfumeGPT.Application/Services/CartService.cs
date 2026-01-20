using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Application.Services
{
	public class CartService : ICartService
	{
		private readonly ICartRepository _cartRepository;
		private readonly ICartItemRepository _cartItemRepository;
		private readonly IGHNService _ghnService;
		private readonly IVoucherRepository _voucherRepository;
		private readonly IAddressService _addressService;
		private readonly IMapper _mapper;

		public CartService(
			ICartRepository cartRepository,
			ICartItemRepository cartItemRepository,
			IGHNService ghnService,
			IAddressService addressService,
			IVoucherRepository voucherRepository,
			IMapper mapper)
		{
			_cartRepository = cartRepository;
			_cartItemRepository = cartItemRepository;
			_ghnService = ghnService;
			_addressService = addressService;
			_voucherRepository = voucherRepository;
			_mapper = mapper;
		}

		public async Task<BaseResponse<GetCartResponse>> GetCartByUserIdAsync(Guid userId, Guid? voucherId)
		{
			try
			{
				var cart = await _cartRepository.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<GetCartResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
				}

				var items = await _cartItemRepository.GetCartItemByCartIdAsync(cart.Id);
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

				var shippingFee = await CalculateShippingFeeAsync(addressResponse.Payload.DistrictId, addressResponse.Payload.WardCode);
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
					totalPrice = await ApplyVoucherDiscountAsync(totalPrice, voucherId.Value);
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

		private static decimal CalculateSubtotal(List<GetCartItemResponse> items)
		{
			return items.Sum(item => item.SubTotal);
		}

		private async Task<decimal?> CalculateShippingFeeAsync(int districtId, string wardCode)
		{
			try
			{
				var calculateShippingFeeRequest = new CalculateFeeRequest
				{
					ToDistrictId = districtId,
					ToWardCode = wardCode
				};

				var shippingFeeResponse = await _ghnService.CalculateShippingFeeAsync(calculateShippingFeeRequest);
				return shippingFeeResponse?.Data?.Total;
			}
			catch
			{
				return null;
			}
		}

		private async Task<decimal> ApplyVoucherDiscountAsync(decimal totalPrice, Guid voucherId)
		{
			try
			{
				var voucher = await _voucherRepository.GetByIdAsync(voucherId);
				if (voucher == null)
				{
					return totalPrice;
				}

				var discountAmount = voucher.DiscountType switch
				{
					Domain.Enums.DiscountType.Percentage => totalPrice * (voucher.DiscountValue / 100m),
					Domain.Enums.DiscountType.FixedAmount => voucher.DiscountValue,
					_ => 0m
				};

				var finalPrice = totalPrice - discountAmount;
				return finalPrice < 0m ? 0m : finalPrice;
			}
			catch
			{
				return totalPrice;
			}
		}

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId)
		{
			var cart = await _cartRepository.GetByUserIdAsync(userId);
			if (cart == null)
			{
				return BaseResponse<string>.Fail("Cart not found", ResponseErrorType.NotFound);
			}
			try
			{
				var items = await _cartItemRepository.GetCartItemByCartIdAsync(cart.Id);
				if (items != null && items.Count > 0)
				{
					var result = await _cartRepository.ClearCartByUserIdAsync(userId);
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
