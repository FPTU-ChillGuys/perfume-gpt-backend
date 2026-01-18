using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Auths;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmailTemplateService _templateService;
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly IAuthRepository _authRepository;
        private readonly IProfileService _profileService;
        private readonly ILoyaltyPointService _loyaltyPointService;

        public AuthService(IEmailTemplateService templateService, UserManager<User> userManager, IEmailService emailService, IAuthRepository authRepository, IProfileService profileService, ILoyaltyPointService loyaltyPointService)
        {
            _templateService = templateService;
            _userManager = userManager;
            _emailService = emailService;
            _authRepository = authRepository;
            _profileService = profileService;
            _loyaltyPointService = loyaltyPointService;
        }

        public async Task<BaseResponse<TokenResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email!);
            if (user == null)
                return BaseResponse<TokenResponse>.Fail("User not found", ResponseErrorType.NotFound);

            if (!user.IsActive)
                return BaseResponse<TokenResponse>.Fail("User is inactive", ResponseErrorType.Forbidden);

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password!);
            if (!isPasswordValid)
                return BaseResponse<TokenResponse>.Fail("Invalid password", ResponseErrorType.Unauthorized);

            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            if (!isEmailConfirmed)
                return BaseResponse<TokenResponse>.Fail("Email not confirmed", ResponseErrorType.Forbidden);

            var tokenResponse = await GenerateTokenResponseAsync(user);
            return BaseResponse<TokenResponse>.Ok(tokenResponse);
        }

        private async Task<TokenResponse> GenerateTokenResponseAsync(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";
            return new TokenResponse()
            {
                AccessToken = await _authRepository.GenerateJwtToken(user, role)
            };
        }

        public async Task<BaseResponse<string>> RegisterAsync(RegisterRequest request, UserRole? role)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return BaseResponse<string>.Fail("Email already exists", ResponseErrorType.Conflict);

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.UserName,
                IsActive = true
            };

            var identityResult = await _userManager.CreateAsync(user, request.Password!);
            if (!identityResult.Succeeded)
                return BaseResponse<string>.Fail(
                    "Failed to create user",
                    ResponseErrorType.BadRequest,
                    identityResult.Errors.Select(e => e.Description).ToList()
                );

            // C#
            var roleString = role?.ToString();
            var roleName = string.IsNullOrWhiteSpace(roleString) ? UserRole.User.ToString() : roleString!;
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
                return BaseResponse<string>.Fail(
                    "Failed to assign role",
                    ResponseErrorType.BadRequest,
                    roleResult.Errors.Select(e => e.Description).ToList()
                );

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Send verification email
            var verifyUrl = QueryHelpers.AddQueryString(
                request.ClientUri!,
                new Dictionary<string, string?>
                {
                    { "email", request.Email! },
                    { "token", token }
                }
            );

            var emailContent = _templateService.GetRegisterTemplate(
                request.FullName ?? request.Email,
                verifyUrl
            );

            await _emailService.SendEmailAsync(request.Email!, "Email Confirmation", emailContent);

            return BaseResponse<string>.Ok(token, "User registered successfully, Please check your email to verify!");

        }

        public async Task<BaseResponse<string>> VerifyEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BaseResponse<string>.Fail("User not found", ResponseErrorType.NotFound);
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return BaseResponse<string>.Fail("failed to verify", ResponseErrorType.InternalError);
            }

            // After successful email confirmation, only create a profile for users with the `User` role.
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => r == UserRole.User.ToString()))
            {
                // If the user is not a regular 'User', skip profile creation but still report email verification success.
                return BaseResponse<string>.Ok("Email verified");
            }

            try
            {
                var profileResult = await _profileService.CreateProfileAsync(new CreateProfileRequest { UserId = user.Id });
                if (!profileResult.Success)
                {
                    // Verification succeeded but profile creation failed - return success with note.
                    return BaseResponse<string>.Ok($"Email verified but profile creation failed: {profileResult.Message}");
                }
                // After profile creation, create initial loyalty point record for the user
                try
                {
                    var lpId = await _loyaltyPointService.CreateLoyaltyPointAsync(new CreateLoyaltyPointRequest { UserId = user.Id });
                    // ignore lpId result; do not fail verification if loyalty point creation fails
                }
                catch
                {
                    // swallow exceptions from loyalty point creation
                }
            }
            catch
            {
                // Do not fail the verification if profile creation throws - still return success.
                return BaseResponse<string>.Ok("Email verified");
            }

            return BaseResponse<string>.Ok("Success");
        }

        public async Task<BaseResponse<TokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.IdToken))
                return BaseResponse<TokenResponse>.Fail("Invalid request", ResponseErrorType.BadRequest);

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            }
            catch
            {
                return BaseResponse<TokenResponse>.Fail("Invalid Google token", ResponseErrorType.BadRequest);
            }

            var email = payload?.Email;
            if (string.IsNullOrWhiteSpace(email))
                return BaseResponse<TokenResponse>.Fail("Google token does not contain an email", ResponseErrorType.BadRequest);

            User? user = null;
            try
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user is null)
                {
                    // Attempt registration via repository; repository returns a created user or null on failure.
                    user = await _authRepository.RegisterViaGoogleAsync(payload);
                    if (user is null)
                        return BaseResponse<TokenResponse>.Fail("Failed to create user account from Google payload", ResponseErrorType.InternalError);
                }
            }
            catch (Exception ex)
            {
                // Surface repository errors as friendly responses
                return BaseResponse<TokenResponse>.Fail("Google registration failed: " + ex.Message, ResponseErrorType.InternalError);
            }

            if (!user.IsActive)
                return BaseResponse<TokenResponse>.Fail("Your account is inactive.", ResponseErrorType.Forbidden);

            // Ensure email is confirmed (repository may have done this already for new users)
            if (!user.EmailConfirmed)
            {
                await _authRepository.ConfirmEmailAsync(user);
            }

            // No need for an unconditional double UpdateAsync; repository ConfirmEmailAsync already updates if needed.
            // Generate your own JWT + refresh token
            var tokenResponse = await GenerateTokenResponseAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var message = roles.Any() ? "Google login successful" : "Google registration and login successful";

            return BaseResponse<TokenResponse>.Ok(tokenResponse, message);
        }
    }
}
