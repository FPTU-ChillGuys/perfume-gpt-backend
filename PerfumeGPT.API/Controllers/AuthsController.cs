using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Auths;
using PerfumeGPT.Application.DTOs.Responses.Auths;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthsController : BaseApiController
	{
		private readonly IAuthService _authService;
		private readonly IUserService _userService;

		public AuthsController(IAuthService authService, IUserService userService)
		{
			_authService = authService;
			_userService = userService;
		}

		[HttpPost("login")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> Login([FromBody] LoginRequest request)
		{
			var validation = ValidateRequestBody<TokenResponse>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.LoginAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("register")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> Register([FromBody] RegisterRequest request)
		{
			var validation = ValidateRequestBody<string>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.RegisterAsync(request, null);
			return HandleResponse(result);
		}

		[HttpPost("admin/register")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> RegisterByAdmin([FromBody] RegisterRequest request, [FromQuery] UserRole role)
		{
			var validation = ValidateRequestBody<string>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.RegisterAsync(request, role);
			return HandleResponse(result);
		}

		[HttpGet("verify-email")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> VerifyEmail([FromQuery] string email, [FromQuery] string token)
		{
			var validation = ValidateRequestBody<string>(email);
			if (validation != null)
			{
				return validation;
			}

			var validationToken = ValidateRequestBody<string>(token);
			if (validationToken != null)
			{
				return validationToken;
			}

			var result = await _authService.VerifyEmailAsync(email, token);
			return HandleResponse(result);
		}


		[HttpPost("google-login")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
		{
			var validation = ValidateRequestBody<TokenResponse>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.LoginWithGoogleAsync(request);
			if (!result.Success)
				return HandleResponse(result);

			return HandleResponse(result);
		}

		[HttpPost("api-token")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> CreateApiToken()
		{
			var userId = GetCurrentUserId();
			var emailResult = await _userService.GetEmailByIdAsync(userId);

			if (emailResult == null)
			{
				return HandleResponse(BaseResponse<TokenResponse>.Fail("User not found", ResponseErrorType.NotFound));
			}
			var tokenResult = await _authService.CreateApiTokenAsync(emailResult?.Payload!);
			return HandleResponse(tokenResult);
		}

		[HttpPost("forgot-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
		{
			var validation = ValidateRequestBody<string>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.ForgotPasswordAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("reset-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> ResetPassword([FromBody] ResetPasswordRequest request)
		{
			var validation = ValidateRequestBody<string>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _authService.ResetPasswordAsync(request);
			return HandleResponse(result);
		}

	}
}
