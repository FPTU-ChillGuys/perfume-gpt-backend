using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICartService
	{
		Task<BaseResponse<GetCartResponse>> GetCartByUserIdAsync(Guid userId, Guid? voucherId);
		Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId);
		Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, Guid? voucherId);
		Task<BaseResponse<string>> ClearCartAsync(Guid userId);
	}
}
