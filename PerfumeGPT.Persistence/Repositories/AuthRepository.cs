using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AuthRepository : IAuthRepository
	{
		private static readonly JwtSecurityTokenHandler TokenHandler = new();

		private readonly UserManager<User> _userManager;
		private readonly ILogger<AuthRepository> _logger;
		private readonly IMediaService _mediaService;
		private readonly string _issuer;
		private readonly string _audience;
		private readonly SigningCredentials _signingCredentials;

		public AuthRepository(
			IConfiguration configuration,
			UserManager<User> userManager,
			ILogger<AuthRepository> logger,
			IMediaService mediaService)
		{
			_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

			var secretKey = configuration["Jwt:Key"]
				  ?? throw new ArgumentNullException("Jwt:Key not found in configuration");
			_issuer = configuration["Jwt:Issuer"]
				  ?? throw new ArgumentNullException("Jwt:Issuer not found in configuration");
			_audience = configuration["Jwt:Audience"]
				  ?? throw new ArgumentNullException("Jwt:Audience not found in configuration");

			var privateKeyPem = secretKey.Replace("\\n", "\n");
			var rsa = RSA.Create();
			rsa.ImportFromPem(privateKeyPem);
			_signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
		}

		public async Task ConfirmEmailAsync(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			user.EmailConfirmed = true;
			await _userManager.UpdateAsync(user);
		}

		public string GenerateJwtToken(User user, string role)
		{
			ArgumentNullException.ThrowIfNull(user);
			if (string.IsNullOrWhiteSpace(role)) throw new ArgumentNullException(nameof(role));

			var claims = new List<Claim>
			{
				new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new("id", user.Id.ToString()),
				new("phoneNumber", user.PhoneNumber ?? string.Empty),
				new("email", user.Email ?? string.Empty),
				new("role", role.ToLower()),
				new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			var token = new JwtSecurityToken(
				issuer: _issuer,
				audience: _audience,
				claims: claims,
				notBefore: DateTime.UtcNow,
			   signingCredentials: _signingCredentials
			);

			return TokenHandler.WriteToken(token);
		}

		public async Task<User> RegisterViaGoogleAsync(GoogleJsonWebSignature.Payload payload)
		{
			if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
			{
				throw new ArgumentException("Google payload is invalid.", nameof(payload));
			}

			var existing = await _userManager.FindByEmailAsync(payload.Email);
			if (existing != null)
			{
				return existing;
			}

			var newUser = User.Create(payload.Name ?? payload.Email, payload.Email, null);

			var tempPassword = GenerateTemporaryPassword(12);
			var createResult = await _userManager.CreateAsync(newUser, tempPassword);
			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(
					$"Failed to create user via Google for {payload.Email}. Errors: {string.Join(" | ", createResult.Errors.Select(e => e.Description))}");
			}

			string defaultRole = UserRole.user.ToString();
			var roleResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
			if (!roleResult.Succeeded)
			{
				_logger.LogWarning(
					"Failed to add role '{Role}' to user {Email}. Errors: {Errors}",
					defaultRole,
					payload.Email,
					string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
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
	}
}
