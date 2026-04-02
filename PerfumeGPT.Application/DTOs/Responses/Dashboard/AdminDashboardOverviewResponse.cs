namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public record AdminDashboardOverviewResponse
	{
		public RevenueSummaryResponse Revenue { get; init; } = new();
		public InventoryLevelsResponse InventoryLevels { get; init; } = new();
		public required List<TopProductResponse> TopProducts { get; init; }
	}
}
