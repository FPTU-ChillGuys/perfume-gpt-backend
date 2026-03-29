using FluentValidation;
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
		private readonly IValidator<ProfileAvtarUploadRequest> _profileAvtarUploadValidator;

		public UsersController(IUserService userService, IMediaService mediaService, IValidator<ProfileAvtarUploadRequest> profileAvtarUploadValidator)
		{
			_userService = userService;
			_mediaService = mediaService;
			_profileAvtarUploadValidator = profileAvtarUploadValidator;
		}

		[HttpGet("me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<UserCredentialsResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<UserCredentialsResponse>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<UserCredentialsResponse>>> GetCurrentUserProfile()
		{
			var userId = GetCurrentUserId();
			var response = await _userService.GetUserCredentialsAsync(userId);
			return HandleResponse(response);
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
		public async Task<ActionResult<BaseResponse<string>>> UploadAvatar([FromForm] ProfileAvtarUploadRequest request)
		{
			var validation = await ValidateRequestAsync(_profileAvtarUploadValidator, request);
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

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> GetEmailById(Guid id)
		{
			var email = await _userService.GetEmailByIdAsync(id);
			return HandleResponse(email);
		}

	}
}
