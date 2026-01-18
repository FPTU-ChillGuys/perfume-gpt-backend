using Google.Apis.Auth;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
    public interface IAuthRepository
    {
        public Task<string> GenerateJwtToken(User user, string role);
        public Task<User> RegisterViaGoogleAsync(GoogleJsonWebSignature.Payload payload);
        public Task ConfirmEmailAsync(User user);
    }
}
