using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Responses.Auths;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<BaseResponse<string>> RegisterAsync(RegisterRequest register, UserRole? role);
        Task<BaseResponse<string>> VerifyEmailAsync(string email, string token);
        Task<BaseResponse<TokenResponse>> LoginAsync(LoginRequest login);
        Task<BaseResponse<TokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request);
    }
}
