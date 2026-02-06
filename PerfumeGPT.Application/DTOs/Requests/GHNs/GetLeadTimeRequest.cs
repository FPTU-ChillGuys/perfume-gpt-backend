namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public class GetLeadTimeRequest
	{
		public int ToDistrictId { get; set; }
		public string ToWardCode { get; set; } = null!;
		public int ServiceId { get; set; }
	}
}
