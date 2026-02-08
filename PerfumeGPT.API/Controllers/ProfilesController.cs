using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Profiles;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Profiles;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProfilesController : BaseApiController
	{
		private readonly IProfileService _profileService;

		public ProfilesController(IProfileService profileService)
		{
			_profileService = profileService;
		}

		[HttpGet("me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<ProfileResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ProfileResponse>>> GetProfile()
		{
			var userId = GetCurrentUserId();
			var result = await _profileService.GetProfileAsync(userId);
			return HandleResponse(result);
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
	}
}
