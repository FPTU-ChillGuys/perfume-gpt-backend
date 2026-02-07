using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LoyaltyPointsController : BaseApiController
	{
		private readonly ILoyaltyPointService _loyaltyPointService;

		public LoyaltyPointsController(ILoyaltyPointService loyaltyPointService)
		{
			_loyaltyPointService = loyaltyPointService;
		}

		#region Admin Endpoints

		[HttpPost("{userId:guid}/plus")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<bool>>> PlusPoints(Guid userId, [FromBody] PointsRequest request)
		{
			var validation = ValidateRequestBody<PointsRequest>(request);
			if (validation != null)
				return validation;

			var result = await _loyaltyPointService.PlusPointAsync(userId, request.Points);
			return result
				? HandleResponse(BaseResponse<bool>.Ok(true, $"Successfully added {request.Points} points"))
				: HandleResponse(BaseResponse<bool>.Fail("Failed to add points", ResponseErrorType.InternalError));
		}

		[HttpPost("{userId:guid}/redeem")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<bool>>> RedeemPoints(Guid userId, [FromBody] PointsRequest request)
		{
			var validation = ValidateRequestBody<PointsRequest>(request);
			if (validation != null)
				return validation;

			var result = await _loyaltyPointService.RedeemPointAsync(userId, request.Points);
			return result
				? HandleResponse(BaseResponse<bool>.Ok(true, $"Successfully redeemed {request.Points} points"))
				: HandleResponse(BaseResponse<bool>.Fail("Failed to redeem points. User may not exist or has insufficient balance.", ResponseErrorType.BadRequest));
		}

		[HttpPost("{userId:guid}")]
		[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<bool>>> CreateLoyaltyPoint(Guid userId)
		{
			var result = await _loyaltyPointService.CreateLoyaltyPointAsync(userId);
			return result
				? HandleResponse(BaseResponse<bool>.Ok(true, "Loyalty point account created"))
				: HandleResponse(BaseResponse<bool>.Fail("Failed to create. User may already have a loyalty account.", ResponseErrorType.Conflict));
		}

		#endregion
	}
}
