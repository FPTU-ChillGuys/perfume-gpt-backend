namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public class GetTopProductsRequest : GetDashboardDateRangeRequest
	{
		public int Top { get; set; } = 10;
	}
}
