using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs.Base
{
	public class GHNApiResponse<T>
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = string.Empty;

		[JsonPropertyName("data")]
		public T Data { get; set; } = default!;
	}
}
