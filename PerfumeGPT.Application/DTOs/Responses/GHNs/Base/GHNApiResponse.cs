using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs.Base
{
	public class GHNApiResponse<T>
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		[JsonPropertyName("data")]
		public T Data { get; set; }
	}
}
