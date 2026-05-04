using PerfumeGPT.Application.DTOs.Commons;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IAdminDashboardRepository
	{
		Task<RevenueSummaryResponse> GetRevenueSummaryAsync(DateTime fromDateUtc, DateTime toDateUtc);
		Task<InventoryLevelsResponse> GetInventoryLevelsAsync(DateTime nowUtc, int expiringWithinDays, SellableStockQueryContext sellable);
		Task<List<TopProductResponse>> GetTopProductsAsync(DateTime fromDateUtc, DateTime toDateUtc, int top);
	}
}
