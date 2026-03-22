namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public class AdminDashboardOverviewResponse
	{
		public RevenueSummaryResponse Revenue { get; set; } = new();
		public InventoryLevelsResponse InventoryLevels { get; set; } = new();
		public List<TopProductResponse> TopProducts { get; set; } = [];
	}
}
