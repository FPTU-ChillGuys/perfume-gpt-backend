using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
    public interface ICartItemService
    {
        Task<BaseResponse<string>> AddToCartAsync(CreateCartItemRequest request);
        Task<BaseResponse<string>> UpdateCart(Guid UserId, Guid CartItemId, UpdateCartItemRequest request);
        Task<BaseResponse<string>> RemoveFromCartAsync(Guid UserIdId, Guid cartItemId);
    }
}
