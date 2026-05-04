using PerfumeGPT.Application.DTOs.Requests.Dashboard;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;

namespace PerfumeGPT.Application.Services
{
	public class AdminDashboardService : IAdminDashboardService
	{
		private readonly IAdminDashboardRepository _dashboardRepository;
		private readonly IUnitOfWork _unitOfWork;

		public AdminDashboardService(IAdminDashboardRepository dashboardRepository, IUnitOfWork unitOfWork)
		{
			_dashboardRepository = dashboardRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<BaseResponse<RevenueSummaryResponse>> GetRevenueAsync(GetDashboardDateRangeRequest request)
		{
			var (fromDate, toDate) = ResolveDateRange(request.FromDate, request.ToDate);
			var response = await _dashboardRepository.GetRevenueSummaryAsync(fromDate, toDate);
			return BaseResponse<RevenueSummaryResponse>.Ok(response);
		}

		public async Task<BaseResponse<InventoryLevelsResponse>> GetInventoryLevelsAsync()
		{
			var storePolicies = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var expiringWithinDays = storePolicies?.BatchExpiringSoonThresholdInDays ?? 30;
			var sellable = await SellableStockContextLoader.LoadAsync(_unitOfWork);
			var response = await _dashboardRepository.GetInventoryLevelsAsync(DateTime.UtcNow, expiringWithinDays, sellable);
			return BaseResponse<InventoryLevelsResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<TopProductResponse>>> GetTopProductsAsync(GetTopProductsRequest request)
		{
			var (fromDate, toDate) = ResolveDateRange(request.FromDate, request.ToDate);
			var top = request.Top <= 0 ? 10 : request.Top;
			var response = await _dashboardRepository.GetTopProductsAsync(fromDate, toDate, top);
			return BaseResponse<List<TopProductResponse>>.Ok(response);
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
