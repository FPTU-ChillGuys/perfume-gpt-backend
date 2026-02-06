using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public class GetLeadTimeResponse
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = string.Empty;

		[JsonPropertyName("data")]
		public LeadTimeData? Data { get; set; }
	}

	public class LeadTimeData
	{
		[JsonPropertyName("leadtime")]
		public long LeadTime { get; set; }

		[JsonPropertyName("leadtime_order")]
		public LeadTimeOrder? LeadTimeOrder { get; set; }
	}

	public class LeadTimeOrder
	{
		[JsonPropertyName("from_estimate_date")]
		public DateTime FromEstimateDate { get; set; }

		[JsonPropertyName("to_estimate_date")]
		public DateTime ToEstimateDate { get; set; }
	}
}
