using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProfilesController : BaseApiController
	{
		private readonly IProfileService _profileService;
		private readonly IMediaService _mediaService;

		public ProfilesController(IProfileService profileService, IMediaService mediaService)
		{
			_profileService = profileService;
			_mediaService = mediaService;
		}

		[HttpPut]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateProfile([FromBody] UpdateProfileRequest request)
		{
			var validation = ValidateRequestBody<UpdateProfileRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();

			var result = await _profileService.UpdateProfileAsync(userId, request);
			return HandleResponse(result);
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

			var result = await _mediaService.GetPrimaryMediaAsync(Domain.Enums.EntityType.User, userId);
			return HandleResponse(result);
		}
	}
}
