using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LoyaltyTransactionsController : BaseApiController
	{
		private readonly ILoyaltyTransactionService _loyaltyTransactionService;

		public LoyaltyTransactionsController(ILoyaltyTransactionService loyaltyTransactionService)
		{
			_loyaltyTransactionService = loyaltyTransactionService;
		}

		[HttpGet("me/history")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>>> GetMyLoyaltyHistory([FromQuery] GetPagedUserLoyaltyTransactionsRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _loyaltyTransactionService.GetLoyaltyHistoryAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("me/total")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<LoyaltyTransactionTotalsResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<LoyaltyTransactionTotalsResponse>>> GetMyLoyaltyTotals()
		{
			var userId = GetCurrentUserId();

			var response = await _loyaltyTransactionService.GetLoyaltyTotalsAsync(userId);
			return HandleResponse(response);
		}

		#region Admin Endpoints
		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>>> GetLoyaltyTransactions([FromQuery] GetPagedLoyaltyTransactionsRequest request)
		{
			var response = await _loyaltyTransactionService.GetPagedLoyaltyTransactionsAsync(request);
			return HandleResponse(response);
		}

		[HttpPost("{userId:guid}/manual-change")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ManualChangePoints([FromRoute] Guid userId, [FromBody] ManualChangeRequest request)
		{
			var response = await _loyaltyTransactionService.ManualChangeAsync(userId, request);
			return HandleResponse(response);
		}
		#endregion
	}
}
