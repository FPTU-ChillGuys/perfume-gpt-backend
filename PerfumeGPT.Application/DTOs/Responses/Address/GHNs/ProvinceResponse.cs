using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.Address.GHNs
{
	public record ProvinceResponse
	{
		[JsonPropertyName("ProvinceID")]
		public int ProvinceID { get; init; }

		[JsonPropertyName("ProvinceName")]
		public required string ProvinceName { get; init; }

		[JsonPropertyName("CountryID")]
		public int CountryID { get; init; }

		[JsonPropertyName("Code")]
		public int Code { get; init; }

		[JsonPropertyName("NameExtension")]
		public List<string>? NameExtension { get; init; }

		[JsonPropertyName("IsEnable")]
		public int IsEnable { get; init; }

		[JsonPropertyName("RegionID")]
		public int RegionID { get; init; }

		[JsonPropertyName("UpdatedBy")]
		public int UpdatedBy { get; init; }

		[JsonPropertyName("CreatedAt")]
		public string? CreatedAt { get; init; }

		[JsonPropertyName("UpdatedAt")]
		public string? UpdatedAt { get; init; }

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; init; }

		[JsonPropertyName("Status")]
		public int Status { get; init; }
	}
}
