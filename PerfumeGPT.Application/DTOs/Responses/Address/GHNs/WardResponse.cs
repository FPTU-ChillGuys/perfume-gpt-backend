using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.Address.GHNs
{
	public record WardResponse
	{
		[JsonPropertyName("WardCode")]
		public required string WardCode { get; init; }

		[JsonPropertyName("DistrictID")]
		public int DistrictID { get; init; }

		[JsonPropertyName("WardName")]
		public required string WardName { get; init; }

		[JsonPropertyName("NameExtension")]
		public required List<string> NameExtension { get; init; }

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; init; }

		[JsonPropertyName("SupportType")]
		public int SupportType { get; init; }

		[JsonPropertyName("Status")]
		public int Status { get; init; }

		[JsonPropertyName("CreatedDate")]
		public string? CreatedDate { get; init; }

		[JsonPropertyName("UpdatedDate")]
		public required string UpdatedDate { get; init; }
	}
}
