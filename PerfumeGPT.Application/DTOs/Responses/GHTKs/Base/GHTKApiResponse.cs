using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHTKs.Base
{
	public record GHTKApiResponse<T>
	{
		public bool Success { get; init; }
		public string? Message { get; init; }
		public T? Data { get; init; }
		[JsonPropertyName("log_id")]
		public required string LogId { get; init; }
	}
}
