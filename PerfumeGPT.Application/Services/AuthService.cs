using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using PerfumeGPT.Application.DTOs.Requests.Auths;
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
		#region Dependencies

		private readonly IEmailTemplateService _templateService;
		private readonly UserManager<User> _userManager;
		private readonly IEmailService _emailService;
		private readonly IAuthRepository _authRepository;
		private readonly IProfileService _profileService;
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;
		private readonly IUserRepository _userRepository;
		private readonly IValidator<RegisterRequest> _registerValidator;
		private readonly IUserVoucherRepository _userVoucherRepository;

		public AuthService(IEmailTemplateService templateService,
			UserManager<User> userManager,
			IEmailService emailService,
			IAuthRepository authRepository,
			IProfileService profileService,
			IUserRepository userRepository,
			IValidator<RegisterRequest> registerValidator,
			IUserVoucherRepository userVoucherRepository,
			ILoyaltyTransactionService loyaltyTransactionService)
		{
			_templateService = templateService;
			_userManager = userManager;
			_emailService = emailService;
			_authRepository = authRepository;
			_profileService = profileService;
			_userRepository = userRepository;
			_registerValidator = registerValidator;
			_userVoucherRepository = userVoucherRepository;
			_loyaltyTransactionService = loyaltyTransactionService;
		}

		#endregion

		private async Task SyncVouchersToUserAsync(User user)
		{
			try
			{
				await _userVoucherRepository.MigrateGuestVouchersAsync(user.Id, user.Email!, user.PhoneNumber!);
			}
			catch
			{
			}
		}

		private async Task TryCreateProfileAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			if (!roles.Any(r => r.Equals(UserRole.user.ToString())))
				return;

			try
			{
				var profileResult = await _profileService.CreateProfileAsync(user.Id);
			}
			catch
			{
			}
		}

		public async Task<BaseResponse<TokenResponse>> LoginAsync(LoginRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Credential!) ?? await _userRepository.FindByPhoneNumberAsync(request.Credential!);
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

		public async Task<BaseResponse<TokenResponse>> CreateApiTokenAsync(string email)
		{
			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
				return BaseResponse<TokenResponse>.Fail("User not found", ResponseErrorType.NotFound);
			if (!user.IsActive)
				return BaseResponse<TokenResponse>.Fail("User is inactive", ResponseErrorType.Forbidden);

			var tokenResponse = await GenerateTokenResponseAsync(user);
			return BaseResponse<TokenResponse>.Ok(tokenResponse);
		}

		private async Task<TokenResponse> GenerateTokenResponseAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			var role = roles.FirstOrDefault() ?? UserRole.user.ToString();
			return new TokenResponse()
			{
				AccessToken = await _authRepository.GenerateJwtToken(user, role)
			};
		}

		public async Task<BaseResponse<string>> RegisterAsync(RegisterRequest request, UserRole? role)
		{
			var validationResults = await _registerValidator.ValidateAsync(request);
			if (validationResults != null)
			{
				var errors = validationResults.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			var existingUser = await _userManager.FindByEmailAsync(request.Email) ?? await _userRepository.FindByPhoneNumberAsync(request.PhoneNumber);
			if (existingUser != null)
				return BaseResponse<string>.Fail("Email/PhoneNumber already exists", ResponseErrorType.Conflict);

			var user = new User
			{
				FullName = request.FullName,
				Email = request.Email,
				PhoneNumber = request.PhoneNumber,
				PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(request.PhoneNumber),
				IsActive = true
			};

			var identityResult = await _userManager.CreateAsync(user, request.Password!);
			if (!identityResult.Succeeded)
				return BaseResponse<string>.Fail(
					"Failed to create user",
					ResponseErrorType.BadRequest,
					[.. identityResult.Errors.Select(e => e.Description)]
				);

			var roleName = role?.ToString() ?? UserRole.user.ToString();
			var roleResult = await _userManager.AddToRoleAsync(user, roleName);
			if (!roleResult.Succeeded)
				return BaseResponse<string>.Fail(
					"Failed to assign role",
					ResponseErrorType.BadRequest,
					[.. roleResult.Errors.Select(e => e.Description)]
				);

			var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
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
				return BaseResponse<string>.Fail("User not found", ResponseErrorType.NotFound);

			var result = await _userManager.ConfirmEmailAsync(user, token);
			if (!result.Succeeded)
				return BaseResponse<string>.Fail("failed to verify", ResponseErrorType.InternalError);

			await TryCreateProfileAsync(user);
			await SyncVouchersToUserAsync(user);
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

			var email = payload.Email;
			if (string.IsNullOrWhiteSpace(email))
				return BaseResponse<TokenResponse>.Fail("Google token does not contain an email", ResponseErrorType.BadRequest);

			var user = await _userManager.FindByEmailAsync(email);
			var isNewRegistration = false;

			if (user is null)
			{
				try
				{
					user = await _authRepository.RegisterViaGoogleAsync(payload);
					if (user is null)
						return BaseResponse<TokenResponse>.Fail("Failed to create user account from Google payload", ResponseErrorType.InternalError);

					isNewRegistration = true;
				}
				catch (Exception ex)
				{
					return BaseResponse<TokenResponse>.Fail("Google registration failed: " + ex.Message, ResponseErrorType.InternalError);
				}
			}

			if (!user.IsActive)
				return BaseResponse<TokenResponse>.Fail("Your account is inactive.", ResponseErrorType.Forbidden);

			if (!user.EmailConfirmed)
				await _authRepository.ConfirmEmailAsync(user);

			if (isNewRegistration)
			{
				try
				{
					await TryCreateProfileAsync(user);
					await SyncVouchersToUserAsync(user);
				}
				catch
				{
				}
			}

			var tokenResponse = await GenerateTokenResponseAsync(user);
			var message = isNewRegistration ? "Google registration and login successful" : "Google login successful";

			return BaseResponse<TokenResponse>.Ok(tokenResponse, message);
		}

		public async Task<BaseResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Email!);
			if (user is null)
				return BaseResponse<string>.Fail("User not found", ResponseErrorType.NotFound);

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var param = new Dictionary<string, string?>
			{
				{"token", token },
				{"email", request.Email!}
			};

			var callback = QueryHelpers.AddQueryString(request.ClientUri!, param);
			var emailContent = _templateService.GetForgotPasswordTemplate(user.UserName ?? "User", callback);

			await _emailService.SendEmailAsync(user.Email!, "Reset Password", emailContent);
			return BaseResponse<string>.Ok(token);
		}

		public async Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Email!);
			if (user is null)
				return BaseResponse<string>.Fail("User not found", ResponseErrorType.NotFound);

			var resetResult = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);
			if (!resetResult.Succeeded)
				return BaseResponse<string>.Fail(
					"Failed to reset password",
					ResponseErrorType.BadRequest,
					resetResult.Errors.Select(e => e.Description).ToList());

			return BaseResponse<string>.Ok("Password reset successfully");
		}
	}
}
