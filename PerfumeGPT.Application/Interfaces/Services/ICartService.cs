using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICartService
	{
		Task<BaseResponse<CartCheckoutResponse>> GetCartForCheckoutAsync(Guid userId, GetCartTotalRequest request);
		Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId);
		Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, GetCartTotalRequest request);
		Task<BaseResponse<string>> ClearCartAsync(Guid userId);
	}
}
