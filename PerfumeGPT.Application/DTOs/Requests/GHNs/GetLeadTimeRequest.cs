namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record GetLeadTimeRequest
	{
		public int ToDistrictId { get; init; }
		public required string ToWardCode { get; init; }
		public int ServiceId { get; init; }
	}
}
