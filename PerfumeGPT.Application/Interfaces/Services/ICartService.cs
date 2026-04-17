using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICartService
	{
		Task<CartCheckoutResponse> GetCartForCheckoutAsync(Guid userId, GetCartTotalRequest request);
		Task<BaseResponse<PreviewPosOrderResponse>> PreviewPosOrderAsync(PreviewPosOrderRequest request);
		Task<(List<CartCheckoutItemDto> Items, decimal Subtotal, decimal FinalAmount, string? Message)> CalculatePricingEngineAsync(
			List<CartCheckoutItemDto> checkoutItems, string? voucherCode, Guid? userId, string? GuestEmailOrPhoneNumber);
		Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId, GetPagedCartItemsRequest request);
		Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, GetCartTotalRequest request);
		Task<BaseResponse<string>> ClearCartAsync(Guid userId, List<Guid>? itemIds, bool saveChanges = true);
	}
}
