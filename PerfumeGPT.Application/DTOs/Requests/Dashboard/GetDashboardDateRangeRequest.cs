namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public class GetDashboardDateRangeRequest
	{
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
	}
}
