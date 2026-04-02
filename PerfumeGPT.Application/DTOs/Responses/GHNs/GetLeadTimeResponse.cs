using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public record GetLeadTimeResponse
	{
		[JsonPropertyName("code")]
		public int Code { get; init; }

		[JsonPropertyName("message")]
		public required string Message { get; init; }

		[JsonPropertyName("data")]
		public LeadTimeData? Data { get; init; }
	}

	public record LeadTimeData
	{
		[JsonPropertyName("leadtime")]
		public long LeadTime { get; init; }

		[JsonPropertyName("leadtime_order")]
		public LeadTimeOrder? LeadTimeOrder { get; init; }
	}

	public record LeadTimeOrder
	{
		[JsonPropertyName("from_estimate_date")]
		public DateTime FromEstimateDate { get; init; }

		[JsonPropertyName("to_estimate_date")]
		public DateTime ToEstimateDate { get; init; }
	}
}
