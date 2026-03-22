using FluentValidation;
using Google.Apis.Auth;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Responses.Auths;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
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
		private readonly IUserRepository _userRepository;
		private readonly IValidator<RegisterRequest> _registerValidator;
		private readonly IUserVoucherRepository _userVoucherRepository;
		private readonly IValidator<LoginRequest> _loginValidator;
		private readonly IMapper _mapper;

		public AuthService(IEmailTemplateService templateService,
			UserManager<User> userManager,
			IEmailService emailService,
			IAuthRepository authRepository,
			IProfileService profileService,
			IUserRepository userRepository,
			IValidator<RegisterRequest> registerValidator,
			IUserVoucherRepository userVoucherRepository,
			IValidator<LoginRequest> loginValidator,
			IMapper mapper)
		{
			_templateService = templateService;
			_userManager = userManager;
			_emailService = emailService;
			_authRepository = authRepository;
			_profileService = profileService;
			_userRepository = userRepository;
			_registerValidator = registerValidator;
			_userVoucherRepository = userVoucherRepository;
			_loginValidator = loginValidator;
			_mapper = mapper;
		}
		#endregion

		public async Task<BaseResponse<TokenResponse>> LoginAsync(LoginRequest request)
		{
			var validationResults = await _loginValidator.ValidateAsync(request);
			if (!validationResults.IsValid)
				throw AppException.BadRequest("Validation failed", [.. validationResults.Errors.Select(e => e.ErrorMessage)]);

			var user = await FindByEmailOrPhoneAsync(request.Credential)
				?? throw AppException.NotFound("User not found");

			user.EnsureActive();

			var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
			if (!isPasswordValid)
				throw AppException.Unauthorized("Invalid password");

			user.EnsureEmailConfirmed();

			var tokenResponse = await GenerateTokenResponseAsync(user);
			return BaseResponse<TokenResponse>.Ok(tokenResponse);
		}

		public async Task<BaseResponse<TokenResponse>> CreateApiTokenAsync(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				throw AppException.BadRequest("Email is required");

			var user = await _userManager.FindByEmailAsync(email) ?? throw AppException.NotFound("User not found");
			user.EnsureActive();

			var tokenResponse = await GenerateTokenResponseAsync(user);
			return BaseResponse<TokenResponse>.Ok(tokenResponse);
		}

		private async Task<TokenResponse> GenerateTokenResponseAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			var role = roles.FirstOrDefault() ?? UserRole.user.ToString();
			return new TokenResponse()
			{
				AccessToken = _authRepository.GenerateJwtToken(user, role)
			};
		}

		public async Task<BaseResponse<string>> RegisterAsync(RegisterRequest request, UserRole? role)
		{
			var validationResults = await _registerValidator.ValidateAsync(request);
			if (!validationResults.IsValid)
				throw AppException.BadRequest("Validation failed", validationResults.Errors.Select(e => e.ErrorMessage).ToList());

			var existingUser = await _userManager.FindByEmailAsync(request.Email) ?? await _userRepository.FindByPhoneNumberAsync(request.PhoneNumber);
			if (existingUser != null)
				throw AppException.Conflict("Email/PhoneNumber already exists");

			var user = _mapper.Map<User>(request);

			var identityResult = await _userManager.CreateAsync(user, request.Password!);
			if (!identityResult.Succeeded)
				throw AppException.BadRequest(
					 "Failed to create user",
					 [.. identityResult.Errors.Select(e => e.Description)]
				 );

			var roleName = role?.ToString() ?? UserRole.user.ToString();
			var roleResult = await _userManager.AddToRoleAsync(user, roleName);
			if (!roleResult.Succeeded)
				throw AppException.BadRequest(
					 "Failed to assign role",
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
			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
				throw AppException.BadRequest("Email and token are required");

			var user = await _userManager.FindByEmailAsync(email) ?? throw AppException.NotFound("User not found");

			var result = await _userManager.ConfirmEmailAsync(user, token);
			if (!result.Succeeded)
				throw AppException.BadRequest("Failed to verify email", [.. result.Errors.Select(e => e.Description)]);

			await TryCreateProfileAsync(user);
			await SyncVouchersToUserAsync(user);
			return BaseResponse<string>.Ok("Success");
		}

		public async Task<BaseResponse<TokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request)
		{
			if (request is null || string.IsNullOrWhiteSpace(request.IdToken))
				throw AppException.BadRequest("Invalid request");

			GoogleJsonWebSignature.Payload payload;
			try
			{
				payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
			}
			catch
			{
				throw AppException.BadRequest("Invalid Google token");
			}

			var email = payload.Email;
			if (string.IsNullOrWhiteSpace(email))
				throw AppException.BadRequest("Google token does not contain an email");

			var user = await _userManager.FindByEmailAsync(email);
			var isNewRegistration = false;

			if (user is null)
			{
				user = await _authRepository.RegisterViaGoogleAsync(payload);
				isNewRegistration = true;
			}

			user.EnsureActive();

			if (!user.EmailConfirmed)
				await _authRepository.ConfirmEmailAsync(user);

			if (isNewRegistration)
			{
				await TryCreateProfileAsync(user);
				await SyncVouchersToUserAsync(user);
			}

			var tokenResponse = await GenerateTokenResponseAsync(user);
			var message = isNewRegistration ? "Google registration and login successful" : "Google login successful";

			return BaseResponse<TokenResponse>.Ok(tokenResponse, message);
		}

		public async Task<BaseResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.ClientUri))
				throw AppException.BadRequest("Email and clientUri are required");

			var user = await _userManager.FindByEmailAsync(request.Email!) ?? throw AppException.NotFound("User not found");
			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var param = new Dictionary<string, string?>
			{
				{"token", token },
			   {"email", request.Email}
			};

			var callback = QueryHelpers.AddQueryString(request.ClientUri, param);
			var emailContent = _templateService.GetForgotPasswordTemplate(user.UserName ?? "User", callback);

			await _emailService.SendEmailAsync(user.Email!, "Reset Password", emailContent);
			return BaseResponse<string>.Ok(token);
		}

		public async Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.Password))
				throw AppException.BadRequest("Email, token and password are required");

			var user = await _userManager.FindByEmailAsync(request.Email!) ?? throw AppException.NotFound("User not found");
			var resetResult = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);
			if (!resetResult.Succeeded)
				throw AppException.BadRequest(
					 "Failed to reset password",
					 resetResult.Errors.Select(e => e.Description).ToList());

			return BaseResponse<string>.Ok("Password reset successfully");
		}

		#region Private Helpers
		private async Task SyncVouchersToUserAsync(User user)
		{
			await _userVoucherRepository.MigrateGuestVouchersAsync(user.Id, user.Email!, user.PhoneNumber!);
		}

		private async Task TryCreateProfileAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			if (!roles.Any(r => r.Equals(UserRole.user.ToString())))
				return;

			await _profileService.CreateProfileAsync(user.Id);
		}

		private async Task<User?> FindByEmailOrPhoneAsync(string credential)
		{
			return await _userManager.FindByEmailAsync(credential)
				?? await _userRepository.FindByPhoneNumberAsync(credential);
		}

		#endregion Private Helpers
	}
}
