using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Requests.GHNs.Address
{
	public class DistrictResponse
	{
		[JsonPropertyName("DistrictID")]
		public int DistrictID { get; set; }

		[JsonPropertyName("ProvinceID")]
		public int ProvinceID { get; set; }

		[JsonPropertyName("DistrictName")]
		public string DistrictName { get; set; }

		[JsonPropertyName("Code")]
		public int Code { get; set; }

		[JsonPropertyName("Type")]
		public int Type { get; set; }

		[JsonPropertyName("SupportType")]
		public int SupportType { get; set; }

		[JsonPropertyName("NameExtension")]
		public List<string> NameExtension { get; set; }

		[JsonPropertyName("IsEnable")]
		public int IsEnable { get; set; }

		[JsonPropertyName("CanUpdateCOD")]
		public bool CanUpdateCOD { get; set; }

		[JsonPropertyName("Status")]
		public int Status { get; set; }

		[JsonPropertyName("CreatedDate")]
		public string CreatedDate { get; set; }

		[JsonPropertyName("UpdatedDate")]
		public string UpdatedDate { get; set; }
	}
}
