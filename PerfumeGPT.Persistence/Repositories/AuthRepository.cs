using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PerfumeGPT.Persistence.Repositories
{
	public class AuthRepository : IAuthRepository
	{
		private static readonly JwtSecurityTokenHandler TokenHandler = new();

		private readonly string _issuer;
		private readonly string _audience;
		private readonly SigningCredentials _signingCredentials;

		public AuthRepository(IConfiguration configuration)
		{
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
	}
}
