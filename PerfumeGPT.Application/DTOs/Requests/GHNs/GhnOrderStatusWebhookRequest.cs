using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record GhnOrderStatusWebhookRequest
	{
		[JsonPropertyName("OrderCode")]
		public required string OrderCode { get; init; }

		[JsonPropertyName("Status")]
		public required string Status { get; init; }

		[JsonPropertyName("Type")]
		public string? Type { get; init; }
	}
}
