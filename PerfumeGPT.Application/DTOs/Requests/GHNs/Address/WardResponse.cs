using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.GHNs.Address
{
	public class WardResponse
	{
		[JsonPropertyName("WardCode")]
		public string WardCode { get; set; } = null!;

		[JsonPropertyName("DistrictID")]
		public int DistrictID { get; set; }

		[JsonPropertyName("WardName")]
		public string WardName { get; set; } = string.Empty;

		[JsonPropertyName("NameExtension")]
		public List<string> NameExtension { get; set; } = new List<string>();

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; set; }

		[JsonPropertyName("SupportType")]
		public int SupportType { get; set; }

		[JsonPropertyName("Status")]
		public int Status { get; set; }

		[JsonPropertyName("CreatedDate")]
		public string CreatedDate { get; set; } = string.Empty;

		[JsonPropertyName("UpdatedDate")]
		public string UpdatedDate { get; set; } = string.Empty;
	}
}
