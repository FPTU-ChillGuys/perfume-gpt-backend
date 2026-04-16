using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Responses.Auths;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System.Security.Cryptography;
using static PerfumeGPT.Domain.Entities.User;

namespace PerfumeGPT.Application.Services
{
	public class AuthService : IAuthService
	{
		#region Dependencies
		private readonly UserManager<User> _userManager;
		private readonly IAuthRepository _authRepository;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IEmailService _emailService;
		private readonly IEmailTemplateService _templateService;
		private readonly IMediaService _mediaService;
		private readonly ILogger<AuthService> _logger;

		public AuthService(
			IEmailTemplateService templateService,
			UserManager<User> userManager,
			IEmailService emailService,
			IAuthRepository authRepository,
			IUserRepository userRepository,
			IUnitOfWork unitOfWork,
			IMediaService mediaService,
			ILogger<AuthService> logger)
		{
			_templateService = templateService;
			_userManager = userManager;
			_emailService = emailService;
			_authRepository = authRepository;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
			_mediaService = mediaService;
			_logger = logger;
		}
		#endregion

		public async Task<BaseResponse<TokenResponse>> LoginAsync(LoginRequest request)
		{
			var user = await _userRepository.FindByPhoneOrEmailAsync(request.Credential)
				  ?? throw AppException.NotFound("User not found");

			user.EnsureActive();

			var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
			if (!isPasswordValid)
				throw AppException.Unauthorized("Invalid password");

			user.EnsureEmailConfirmed();

			var tokenResponse = await GenerateTokenResponseAsync(user);
			return BaseResponse<TokenResponse>.Ok(tokenResponse);
		}

		public async Task<BaseResponse<TokenResponse>> CreateApiTokenAsync(Guid userId)
		{
			var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw AppException.NotFound("User not found");
			user.EnsureActive();

			var tokenResponse = await GenerateTokenResponseAsync(user);
			return BaseResponse<TokenResponse>.Ok(tokenResponse);
		}

		public async Task<BaseResponse<string>> RegisterAsync(RegisterRequest request, UserRole? role)
		{
			var existingUser = await _userManager.FindByEmailAsync(request.Email)
			  ?? await _userRepository.FindByPhoneNumberAsync(request.PhoneNumber);
			if (existingUser != null)
				throw AppException.Conflict("Email/PhoneNumber already exists");

			var creationDetails = new UserCreationDetails(
				request.FullName ?? request.Email,
				request.Email,
				request.PhoneNumber
			);
			var user = User.Create(creationDetails);

			var identityResult = await _userManager.CreateAsync(user, request.Password!);
			if (!identityResult.Succeeded)
				throw AppException.BadRequest(
					"Failed to create user",
					[.. identityResult.Errors.Select(e => e.Description)]);

			var roleName = role?.ToString() ?? UserRole.user.ToString();
			var roleResult = await _userManager.AddToRoleAsync(user, roleName);
			if (!roleResult.Succeeded)
				throw AppException.BadRequest(
					"Failed to assign role",
					[.. roleResult.Errors.Select(e => e.Description)]);

			var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
			var verifyUrl = QueryHelpers.AddQueryString(
				request.ClientUri!,
				new Dictionary<string, string?>
				{
					{ "email", request.Email! },
					{ "token", token }
				});

			var emailContent = _templateService.GetRegisterTemplate(
				request.FullName ?? request.Email, verifyUrl);

			await _emailService.SendEmailAsync(request.Email!, "[PerfumeGPT] Xác nhận địa chỉ email", emailContent);
			return BaseResponse<string>.Ok(token, "User registered successfully. Please check your email to verify!");
		}

		public async Task<BaseResponse<string>> VerifyEmailAsync(VerifyEmailRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var user = await _userManager.FindByEmailAsync(request.Email) ?? throw AppException.NotFound("User not found");

				var result = await _userManager.ConfirmEmailAsync(user, request.Token);
				if (!result.Succeeded)
					throw AppException.BadRequest("Failed to verify email", [.. result.Errors.Select(e => e.Description)]);

				await _unitOfWork.UserVouchers.MigrateGuestVouchersAsync(
					user.Id,
					user.Email ?? string.Empty,
					user.PhoneNumber ?? string.Empty);

				return BaseResponse<string>.Ok("Success");
			});
		}

		public async Task<BaseResponse<TokenResponse>> LoginWithGoogleAsync(GoogleLoginRequest request)
		{
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

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var user = await _userManager.FindByEmailAsync(email);
				var isNewRegistration = false;

				if (user is null)
				{
					user = await CreateGoogleUserAsync(payload);
					isNewRegistration = true;
				}

				user.EnsureActive();

				if (!user.EmailConfirmed)
				{
					user.EmailConfirmed = true;
					await _userManager.UpdateAsync(user);
				}

				await _unitOfWork.UserVouchers.MigrateGuestVouchersAsync(
					user.Id,
					user.Email ?? string.Empty,
					user.PhoneNumber ?? string.Empty);

				var tokenResponse = await GenerateTokenResponseAsync(user);
				var message = isNewRegistration ? "Google registration and login successful" : "Google login successful";

				return BaseResponse<TokenResponse>.Ok(tokenResponse, message);
			});
		}

		public async Task<BaseResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Email!) ?? throw AppException.NotFound("User not found");
			var token = await _userManager.GeneratePasswordResetTokenAsync(user);
			var param = new Dictionary<string, string?>
		{
			{"token", token },
			{"email", request.Email}
		};

			var callback = QueryHelpers.AddQueryString(request.ClientUri, param);
			var emailContent = _templateService.GetForgotPasswordTemplate(user.UserName ?? "User", callback);

			await _emailService.SendEmailAsync(user.Email!, "[PerfumeGPT] Yêu cầu đặt lại mật khẩu", emailContent);
			return BaseResponse<string>.Ok(token);
		}

		public async Task<BaseResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
		{
			var user = await _userManager.FindByEmailAsync(request.Email!) ?? throw AppException.NotFound("User not found");

			var resetResult = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);
			if (!resetResult.Succeeded)
				throw AppException.BadRequest(
					 "Failed to reset password",
					 resetResult.Errors.Select(e => e.Description).ToList());

			return BaseResponse<string>.Ok("Password reset successfully");
		}

		#region Private Helpers
		private async Task<TokenResponse> GenerateTokenResponseAsync(User user)
		{
			var roles = await _userManager.GetRolesAsync(user);
			var role = roles.FirstOrDefault() ?? UserRole.user.ToString();
			return new TokenResponse()
			{
				AccessToken = _authRepository.GenerateJwtToken(user, role)
			};
		}

		private async Task<User> CreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
		{
			var creationDetails = new UserCreationDetails(
				FullName: payload.Name ?? payload.Email,
				Email: payload.Email,
				PhoneNumber: null
			);
			var newUser = User.Create(creationDetails);
			var tempPassword = GenerateTemporaryPassword(12);

			var createResult = await _userManager.CreateAsync(newUser, tempPassword);
			if (!createResult.Succeeded)
			{
				throw AppException.Internal(
					$"Failed to create user via Google. Errors: {string.Join(" | ", createResult.Errors.Select(e => e.Description))}");
			}

			string defaultRole = UserRole.user.ToString();
			var roleResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
			if (!roleResult.Succeeded)
			{
				_logger.LogWarning("Failed to add role '{Role}' to user {Email}. Errors: {Errors}",
					defaultRole, payload.Email, string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
			}

			if (!string.IsNullOrWhiteSpace(payload.Picture))
			{
				var avatarCreated = await _mediaService.CreateProfileAvatarFromUrlAsync(
					newUser.Id,
					payload.Picture,
					$"{newUser.FullName}'s profile picture");

				if (!avatarCreated)
				{
					_logger.LogWarning("Failed to create profile picture for user {Email}", payload.Email);
				}
			}

			return newUser;
		}

		private static string GenerateTemporaryPassword(int length = 12)
		{
			if (length < 8) length = 8;

			const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			const string lower = "abcdefghijklmnopqrstuvwxyz";
			const string digits = "0123456789";
			const string special = "!@#$%^&*()-_=+[]{};:,.<>?";

			var all = upper + lower + digits + special;
			var chars = new List<char>
		{
			upper[RandomNumberGenerator.GetInt32(upper.Length)],
			lower[RandomNumberGenerator.GetInt32(lower.Length)],
			digits[RandomNumberGenerator.GetInt32(digits.Length)],
			special[RandomNumberGenerator.GetInt32(special.Length)]
		};

			for (int i = chars.Count; i < length; i++)
			{
				chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
			}

			var arr = chars.ToArray();
			for (int i = arr.Length - 1; i > 0; i--)
			{
				int j = RandomNumberGenerator.GetInt32(i + 1);
				(arr[j], arr[i]) = (arr[i], arr[j]);
			}

			return new string(arr);
		}
		#endregion
	}
}
