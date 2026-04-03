using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
	public record GetStoresResponse
	{
		[JsonPropertyName("last_offset")]
		public int LastOffset { get; init; }

		[JsonPropertyName("shops")]
		public List<GhnStoreDto>? Shops { get; init; }
	}

	public record GhnStoreDto
	{
		[JsonPropertyName("name")]
		public string? Name { get; init; }

		[JsonPropertyName("phone")]
		public string? Phone { get; init; }

		[JsonPropertyName("address")]
		public string? Address { get; init; }

		[JsonPropertyName("ward_code")]
		public string? WardCode { get; init; }

		[JsonPropertyName("district_id")]
		public int DistrictId { get; init; }
	}
}
