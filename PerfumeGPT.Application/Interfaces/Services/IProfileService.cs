using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Profiles;

namespace PerfumeGPT.Application.Interfaces.Services
{
    public interface IProfileService
    {
        Task<BaseResponse<string>> CreateProfileAsync(CreateProfileRequest request);
        Task<BaseResponse<string>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<BaseResponse<ProfileResponse>> GetProfileAsync(Guid userId);
    }
}
