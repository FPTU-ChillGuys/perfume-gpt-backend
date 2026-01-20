using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICartItemService
	{
		Task<BaseResponse<string>> AddToCartAsync(Guid userId, CreateCartItemRequest request);
		Task<BaseResponse<string>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request);
		Task<BaseResponse<string>> RemoveFromCartAsync(Guid userId, Guid cartItemId);
	}
}
