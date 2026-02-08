using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHTKs.Base
{
	public class GHTKApiResponse<T>
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public T Data { get; set; } = default!;
		[JsonPropertyName("log_id")]
		public string LogId { get; set; } = string.Empty;
	}
}
