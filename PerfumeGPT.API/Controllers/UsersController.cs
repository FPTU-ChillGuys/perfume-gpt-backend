using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Users;
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
		private readonly IValidator<UpdateUserBasicInfoRequest> _updateUserBasicInfoValidator;

		public UsersController(
			IUserService userService,
			IMediaService mediaService,
			IValidator<ProfileAvtarUploadRequest> profileAvtarUploadValidator,
			IValidator<UpdateUserBasicInfoRequest> updateUserBasicInfoValidator)
		{
			_userService = userService;
			_mediaService = mediaService;
			_profileAvtarUploadValidator = profileAvtarUploadValidator;
			_updateUserBasicInfoValidator = updateUserBasicInfoValidator;
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

		[HttpPut("me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCurrentUserBasicInfo([FromBody] UpdateUserBasicInfoRequest request)
		{
			var validation = await ValidateRequestAsync(_updateUserBasicInfoValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _userService.UpdateUserBasicInfoAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("for-pos")]
		[ProducesResponseType(typeof(BaseResponse<CustomerForPosResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<CustomerForPosResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<CustomerForPosResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<CustomerForPosResponse>>> GetCustomerForPos([FromQuery] string phoneOrEmail)
		{
			var response = await _userService.GetCustomerForPosAsync(phoneOrEmail);
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

		[HttpGet("staff-manage")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<List<StaffManageItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<StaffManageItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<StaffManageItem>>>> GetStaffForManagement()
		{
			var response = await _userService.GetStaffForManagementAsync();
			return HandleResponse(response);
		}

		[HttpPut("staff/{staffId:guid}/inactive")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> InactiveStaff([FromRoute] Guid staffId)
		{
			var response = await _userService.InactiveStaffAsync(staffId);
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
		public async Task<ActionResult<BaseResponse<string>>> GetEmailById([FromRoute] Guid id)
		{
			var email = await _userService.GetEmailByIdAsync(id);
			return HandleResponse(email);
		}

	}
}
