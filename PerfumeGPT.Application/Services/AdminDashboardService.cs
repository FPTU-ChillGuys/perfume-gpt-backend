using PerfumeGPT.Application.DTOs.Requests.Dashboard;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class AdminDashboardService : IAdminDashboardService
	{
		private readonly IAdminDashboardRepository _dashboardRepository;

		public AdminDashboardService(IAdminDashboardRepository dashboardRepository)
		{
			_dashboardRepository = dashboardRepository;
		}

		public async Task<BaseResponse<RevenueSummaryResponse>> GetRevenueAsync(GetDashboardDateRangeRequest request)
		{
			var (fromDate, toDate) = ResolveDateRange(request.FromDate, request.ToDate);
			var response = await _dashboardRepository.GetRevenueSummaryAsync(fromDate, toDate);
			return BaseResponse<RevenueSummaryResponse>.Ok(response);
		}

		public async Task<BaseResponse<InventoryLevelsResponse>> GetInventoryLevelsAsync(GetInventoryLevelsRequest request)
		{
			var expiringWithinDays = request.ExpiringWithinDays <= 0 ? 30 : request.ExpiringWithinDays;
			var response = await _dashboardRepository.GetInventoryLevelsAsync(DateTime.UtcNow, expiringWithinDays);
			return BaseResponse<InventoryLevelsResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<TopProductResponse>>> GetTopProductsAsync(GetTopProductsRequest request)
		{
			var (fromDate, toDate) = ResolveDateRange(request.FromDate, request.ToDate);
			var top = request.Top <= 0 ? 10 : request.Top;
			var response = await _dashboardRepository.GetTopProductsAsync(fromDate, toDate, top);
			return BaseResponse<List<TopProductResponse>>.Ok(response);
		}

		public async Task<BaseResponse<AdminDashboardOverviewResponse>> GetOverviewAsync(GetDashboardOverviewRequest request)
		{
			var (fromDate, toDate) = ResolveDateRange(request.FromDate, request.ToDate);
			var expiringWithinDays = request.ExpiringWithinDays <= 0 ? 30 : request.ExpiringWithinDays;
			var top = request.Top <= 0 ? 10 : request.Top;

			var payload = new AdminDashboardOverviewResponse
			{
				Revenue = await _dashboardRepository.GetRevenueSummaryAsync(fromDate, toDate),
				InventoryLevels = await _dashboardRepository.GetInventoryLevelsAsync(DateTime.UtcNow, expiringWithinDays),
				TopProducts = await _dashboardRepository.GetTopProductsAsync(fromDate, toDate, top)
			};

			return BaseResponse<AdminDashboardOverviewResponse>.Ok(payload);
		}

		private static (DateTime FromDateUtc, DateTime ToDateUtc) ResolveDateRange(DateTime? fromDate, DateTime? toDate)
		{
			var to = toDate?.ToUniversalTime() ?? DateTime.UtcNow;
			var from = fromDate?.ToUniversalTime() ?? to.AddDays(-30);

			if (from > to)
			{
				(from, to) = (to, from);
			}

			return (from, to);
		}
	}
}
