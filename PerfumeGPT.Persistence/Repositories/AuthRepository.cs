using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AuthRepository : IAuthRepository
	{
		private readonly UserManager<User> _userManager;
		private readonly PerfumeDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly ILogger<AuthRepository> _logger;
		private readonly IMediaService _mediaService;
		private readonly string _secretKey;
		private readonly string _issuer;
		private readonly string _audience;

		public AuthRepository(
			PerfumeDbContext context,
			IConfiguration configuration,
			UserManager<User> userManager,
			ILogger<AuthRepository> logger,
			IMediaService mediaService)
		{
			_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

			_secretKey = _configuration["Jwt:Key"]
				?? throw new ArgumentNullException("Jwt:Key not found in configuration");
			_issuer = _configuration["Jwt:Issuer"]
				?? throw new ArgumentNullException("Jwt:Issuer not found in configuration");
			_audience = _configuration["Jwt:Audience"]
				?? throw new ArgumentNullException("Jwt:Audience not found in configuration");
		}

		public async Task ConfirmEmailAsync(User user)
		{
			ArgumentNullException.ThrowIfNull(user);
			user.EmailConfirmed = true;
			await _userManager.UpdateAsync(user);
		}

		public async Task<string> GenerateJwtToken(User user, string role)
		{
			ArgumentNullException.ThrowIfNull(user);
			if (string.IsNullOrWhiteSpace(role)) throw new ArgumentNullException(nameof(role));

			var claims = new List<Claim>
			{
				new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
				new Claim("id", user.Id.ToString()),
				new Claim("phoneNumber", user.PhoneNumber ?? string.Empty),
				new Claim("email", user.Email ?? string.Empty),
				new Claim("role", role.ToLower()),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			//var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
			//var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var rsa = RSA.Create();
			rsa.ImportFromPem(_secretKey);
			var key = new RsaSecurityKey(rsa);
			var creds = new SigningCredentials(
				key,
				SecurityAlgorithms.RsaSha256 // RS256
			);

			var token = new JwtSecurityToken(
				issuer: _issuer,
				audience: _audience,
				claims: claims,
				notBefore: DateTime.UtcNow,
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}

		public async Task<User> RegisterViaGoogleAsync(GoogleJsonWebSignature.Payload payload)
		{
			if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
			{
				_logger.LogWarning("RegisterViaGoogleAsync called with null/invalid payload.");
				return null!;
			}

			try
			{
				var existing = await _userManager.FindByEmailAsync(payload.Email);
				if (existing != null)
				{
					return existing;
				}

				var newUser = new User
				{
					Email = payload.Email,
					UserName = payload.Email,
					FullName = payload.Name ?? string.Empty,
					EmailConfirmed = true,
					IsActive = true
				};

				var tempPassword = GenerateTemporaryPassword(12);
				var createResult = await _userManager.CreateAsync(newUser, tempPassword);
				if (!createResult.Succeeded)
				{
					_logger.LogWarning(
						"Failed to create user via Google for {Email}. Errors: {Errors}",
						payload.Email,
						string.Join(" | ", createResult.Errors.Select(e => e.Description)));

					return null!;
				}

				string defaultRole = UserRole.user.ToString();
				var roleExists = await _context.Roles.AnyAsync(r => r.Name == defaultRole);
				if (!roleExists)
				{
					_logger.LogWarning("Default role '{Role}' does not exist. Skipping role assignment for user {Email}.", defaultRole, payload.Email);
				}
				else
				{
					var roleResult = await _userManager.AddToRoleAsync(newUser, defaultRole);
					if (!roleResult.Succeeded)
					{
						_logger.LogWarning(
							"Failed to add role '{Role}' to user {Email}. Errors: {Errors}",
							defaultRole,
							payload.Email,
							string.Join(" | ", roleResult.Errors.Select(e => e.Description)));
					}
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
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error in RegisterViaGoogleAsync for email {Email}", payload.Email);
				return null!;
			}
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
