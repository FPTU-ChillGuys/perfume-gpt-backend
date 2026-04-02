using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs.Base
{
	public record GHNApiResponse<T>
	{
		[JsonPropertyName("code")]
		public int Code { get; init; }

		[JsonPropertyName("message")]
		public required string Message { get; init; }

		[JsonPropertyName("data")]
		public T? Data { get; init; }
	}
}
