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

		[HttpGet("me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<GetLoyaltyPointResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<GetLoyaltyPointResponse>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<GetLoyaltyPointResponse>>> GetCurrentUserLoyaltyPoints()
		{
			var userId = GetCurrentUserId();
			var response = await _loyaltyTransactionService.GetLoyaltyPointsAsync(userId);
			return HandleResponse(response);
		}

		#region Admin Endpoints

		[HttpPost("{userId:guid}/plus")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<bool>>> PlusPoints(Guid userId, [FromBody] PointsRequest request)
		{
			var validation = ValidateRequestBody<PointsRequest>(request);
			if (validation != null)
				return validation;

			var result = await _loyaltyTransactionService.PlusPointAsync(userId, request.Points, null);
			return result
				? HandleResponse(BaseResponse<bool>.Ok(true, $"Successfully added {request.Points} points"))
				: HandleResponse(BaseResponse<bool>.Fail("Failed to add points", ResponseErrorType.InternalError));
		}

		[HttpPost("{userId:guid}/redeem")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<bool>>> RedeemPoints(Guid userId, [FromBody] PointsRequest request)
		{
			var validation = ValidateRequestBody<PointsRequest>(request);
			if (validation != null)
				return validation;

			var result = await _loyaltyTransactionService.RedeemPointAsync(userId, request.Points, null, null);
			return result
				? HandleResponse(BaseResponse<bool>.Ok(true, $"Successfully redeemed {request.Points} points"))
				: HandleResponse(BaseResponse<bool>.Fail("Failed to redeem points. User may not exist or has insufficient balance.", ResponseErrorType.BadRequest));
		}

		#endregion
	}
}
