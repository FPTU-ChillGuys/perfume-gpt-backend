using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<Guid> CreateCartAsync(CreateCartRequest request);
        Task<BaseResponse<PagedResult<GetCartItemResponse>>> GetPagedCartItemsAsync(Guid UserId, GetPagedCartItemsRequest request);
    }
}
