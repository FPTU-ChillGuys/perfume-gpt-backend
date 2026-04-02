namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public record GetDashboardDateRangeRequest
	{
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
	}
}
