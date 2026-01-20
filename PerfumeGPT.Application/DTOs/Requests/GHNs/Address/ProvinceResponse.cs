using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.GHNs.Address
{
	public class ProvinceResponse
	{
		[JsonPropertyName("ProvinceID")]
		public int ProvinceID { get; set; }

		[JsonPropertyName("ProvinceName")]
		public string ProvinceName { get; set; } = string.Empty;

		[JsonPropertyName("CountryID")]
		public int CountryID { get; set; }

		[JsonPropertyName("Code")]
		public int Code { get; set; }

		[JsonPropertyName("NameExtension")]
		public List<string> NameExtension { get; set; } = new List<string>();

		[JsonPropertyName("IsEnable")]
		public int IsEnable { get; set; }

		[JsonPropertyName("RegionID")]
		public int RegionID { get; set; }

		[JsonPropertyName("UpdatedBy")]
		public int UpdatedBy { get; set; }

		[JsonPropertyName("CreatedAt")]
		public string CreatedAt { get; set; } = string.Empty;

		[JsonPropertyName("UpdatedAt")]
		public string UpdatedAt { get; set; } = string.Empty;

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; set; }

		[JsonPropertyName("Status")]
		public int Status { get; set; }
	}
}
