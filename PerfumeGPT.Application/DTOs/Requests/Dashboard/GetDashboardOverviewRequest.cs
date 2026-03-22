namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public class GetDashboardOverviewRequest : GetTopProductsRequest
	{
		public int ExpiringWithinDays { get; set; } = 30;
	}
}
