using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Dashboard;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Dashboard;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/admin/dashboard")]
	[ApiController]
	[Authorize(Roles = "admin")]
	public class AdminDashboardController : BaseApiController
	{
		private readonly IAdminDashboardService _adminDashboardService;

		public AdminDashboardController(IAdminDashboardService adminDashboardService)
		{
			_adminDashboardService = adminDashboardService;
		}

		[HttpGet("revenue")]
		[ProducesResponseType(typeof(BaseResponse<RevenueSummaryResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<RevenueSummaryResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<RevenueSummaryResponse>>> GetRevenue([FromQuery] GetDashboardDateRangeRequest request)
		{
			var response = await _adminDashboardService.GetRevenueAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("inventory-levels")]
		[ProducesResponseType(typeof(BaseResponse<InventoryLevelsResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<InventoryLevelsResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<InventoryLevelsResponse>>> GetInventoryLevels([FromQuery] GetInventoryLevelsRequest request)
		{
			var response = await _adminDashboardService.GetInventoryLevelsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("top-products")]
		[ProducesResponseType(typeof(BaseResponse<List<TopProductResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<TopProductResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<TopProductResponse>>>> GetTopProducts([FromQuery] GetTopProductsRequest request)
		{
			var response = await _adminDashboardService.GetTopProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("overview")]
		[ProducesResponseType(typeof(BaseResponse<AdminDashboardOverviewResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<AdminDashboardOverviewResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<AdminDashboardOverviewResponse>>> GetOverview([FromQuery] GetDashboardOverviewRequest request)
		{
			var response = await _adminDashboardService.GetOverviewAsync(request);
			return HandleResponse(response);
		}
	}
}
