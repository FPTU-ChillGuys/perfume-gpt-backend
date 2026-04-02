using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public record GetOrderInfoTokenResponse
	{
		[JsonPropertyName("token")]
		public required string Token { get; init; }
	}
}
