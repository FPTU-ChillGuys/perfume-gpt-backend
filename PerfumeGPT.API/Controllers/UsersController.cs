using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Users;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : BaseApiController
	{
		private readonly IUserService _userService;
		private readonly IMediaService _mediaService;

		public UsersController(IUserService userService, IMediaService mediaService)
		{
			_userService = userService;
			_mediaService = mediaService;
		}

		[HttpGet("staff-lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<StaffLookupItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<StaffLookupItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<StaffLookupItem>>>> GetStaffLookup()
		{
			var response = await _userService.GetStaffLookupAsync();
			return HandleResponse(response);
		}

		[HttpPost("avatar")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UploadAvatar([FromForm] UploadProfileAvatarRequest request)
		{
			var validation = ValidateRequestBody<UploadProfileAvatarRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();

			var result = await _mediaService.UploadProfileAvatarAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpDelete("avatar")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAvatar()
		{
			var userId = GetCurrentUserId();

			var result = await _mediaService.DeleteProfileAvatarAsync(userId);
			return HandleResponse(result);
		}

		[HttpGet("avatar")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<MediaResponse?>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<MediaResponse>>> GetAvatar()
		{
			var userId = GetCurrentUserId();

			var result = await _mediaService.GetPrimaryMediaAsync(EntityType.User, userId);
			return HandleResponse(result);
		}
	}
}
