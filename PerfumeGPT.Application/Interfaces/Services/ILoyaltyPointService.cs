using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;

namespace PerfumeGPT.Application.Interfaces.Services
{
    public interface ILoyaltyPointService
    {
        Task<string> CreateLoyaltyPointAsync(CreateLoyaltyPointRequest request);
        Task<int> PlusPointAsync(Guid userId, int points);
        Task<int> UpdateLoyaltyPointAsync(Guid userId, UpdateLoyaltyPointRequest request);
    }
}
