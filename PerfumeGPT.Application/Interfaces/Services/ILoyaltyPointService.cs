using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.LoyaltyPoints;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ILoyaltyPointService
	{
		Task<BaseResponse<LoyaltyPointResponse>> GetLoyaltyPointsAsync(Guid userId);
		Task<bool> CreateLoyaltyPointAsync(Guid userId, bool saveChanges = true);
		Task<bool> PlusPointAsync(Guid userId, int points, bool saveChanges = true);
		Task<bool> RedeemPointAsync(Guid userId, int points, bool saveChanges = true);
	}
}
