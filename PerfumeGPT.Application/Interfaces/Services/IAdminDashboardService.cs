using PerfumeGPT.Application.DTOs.Requests.Dashboard;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IAdminDashboardService
	{
		Task<BaseResponse<RevenueSummaryResponse>> GetRevenueAsync(GetDashboardDateRangeRequest request);
		Task<BaseResponse<InventoryLevelsResponse>> GetInventoryLevelsAsync(GetInventoryLevelsRequest request);
		Task<BaseResponse<List<TopProductResponse>>> GetTopProductsAsync(GetTopProductsRequest request);
		Task<BaseResponse<AdminDashboardOverviewResponse>> GetOverviewAsync(GetDashboardOverviewRequest request);
	}
}
