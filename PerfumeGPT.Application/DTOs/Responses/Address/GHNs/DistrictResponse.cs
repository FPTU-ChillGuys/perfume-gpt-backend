using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.Address.GHNs
{
	public record DistrictResponse
	{
		[JsonPropertyName("DistrictID")]
		public int DistrictID { get; init; }

		[JsonPropertyName("ProvinceID")]
		public int ProvinceID { get; init; }

		[JsonPropertyName("DistrictName")]
		public required string DistrictName { get; init; }

		[JsonPropertyName("Code")]
		public int Code { get; init; }

		[JsonPropertyName("Type")]
		public int Type { get; init; }

		[JsonPropertyName("SupportType")]
		public int SupportType { get; init; }

		[JsonPropertyName("NameExtension")]
		public required List<string> NameExtension { get; init; }

		[JsonPropertyName("IsEnable")]
		public int IsEnable { get; init; }

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; init; }

		[JsonPropertyName("Status")]
		public int Status { get; init; }

		[JsonPropertyName("CreatedDate")]
		public string? CreatedDate { get; init; }

		[JsonPropertyName("UpdatedDate")]
		public required string UpdatedDate { get; init; }
	}
}
