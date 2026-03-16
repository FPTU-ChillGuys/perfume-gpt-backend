using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ILoyaltyTransactionService
	{
		Task<BaseResponse<GetLoyaltyPointResponse>> GetLoyaltyPointsAsync(Guid userId);
		Task<bool> PlusPointAsync(Guid userId, int points, Guid? orderId, bool saveChanges = true);
		Task<bool> RedeemPointAsync(Guid userId, int points, Guid? voucherId, Guid? orderId, bool saveChanges = true);
	}
}
