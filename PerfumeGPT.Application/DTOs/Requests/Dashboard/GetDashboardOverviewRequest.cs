namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public record GetDashboardOverviewRequest : GetTopProductsRequest
	{
		public int TopProductsCount { get; init; } = 10;
		public int ExpiringWithinDays { get; init; } = 30;
	}
}
