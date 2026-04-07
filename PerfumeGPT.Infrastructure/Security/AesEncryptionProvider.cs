using Microsoft.Extensions.Configuration;
using PerfumeGPT.Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace PerfumeGPT.Infrastructure.Security
{
	public class AesEncryptionProvider : IEncryptionProvider
	{
		private readonly byte[] _key;

		public AesEncryptionProvider(IConfiguration configuration)
		{
			var configuredKey = configuration["Encryption:Key"]
				?? Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
				?? throw new InvalidOperationException("Encryption key is not configured.");

			using var sha256 = SHA256.Create();
			_key = sha256.ComputeHash(Encoding.UTF8.GetBytes(configuredKey));
		}

		public string? Encrypt(string? plainText)
		{
			if (string.IsNullOrWhiteSpace(plainText))
				return plainText;

			using var aes = Aes.Create();
			aes.Key = _key;
			aes.GenerateIV();

			using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			var plainBytes = Encoding.UTF8.GetBytes(plainText);
			var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

			var result = new byte[aes.IV.Length + cipherBytes.Length];
			Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
			Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

			return Convert.ToBase64String(result);
		}

		public string? Decrypt(string? cipherText)
		{
			if (string.IsNullOrWhiteSpace(cipherText))
				return cipherText;

			var fullCipher = Convert.FromBase64String(cipherText);
			using var aes = Aes.Create();
			aes.Key = _key;

			var iv = new byte[aes.BlockSize / 8];
			var cipherBytes = new byte[fullCipher.Length - iv.Length];

			Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
			Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

			aes.IV = iv;
			using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
			var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

			return Encoding.UTF8.GetString(plainBytes);
		}
	}
}
