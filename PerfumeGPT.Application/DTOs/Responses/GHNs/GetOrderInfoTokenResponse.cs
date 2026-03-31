using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public class GetOrderInfoTokenResponse
	{
		[JsonPropertyName("token")]
		public string Token { get; set; } = string.Empty;
	}
}
