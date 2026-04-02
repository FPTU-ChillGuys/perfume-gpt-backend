namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public record GetTopProductsRequest : GetDashboardDateRangeRequest
	{
		public int Top { get; init; } = 10;
	}
}
