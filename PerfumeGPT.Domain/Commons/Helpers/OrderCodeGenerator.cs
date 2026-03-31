using PerfumeGPT.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace PerfumeGPT.Domain.Commons.Helpers
{
	public static class OrderCodeGenerator
	{
		private const string AllowedChars = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

		public static string Generate(OrderType type)
		{
			var datePart = DateTime.UtcNow.ToString("yyMMdd");

			var randomPart = GenerateRandomString(4);

			var channelPrefix = type == OrderType.Online ? "W" : "S";

			return $"PF{channelPrefix}-{datePart}-{randomPart}";
		}

		private static string GenerateRandomString(int length)
		{
			var result = new StringBuilder(length);
			var buffer = new byte[length];

			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(buffer);
			}

			foreach (var b in buffer)
			{
				result.Append(AllowedChars[b % AllowedChars.Length]);
			}

			return result.ToString();
		}
	}
}
