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

		public AuthsController(IAuthService authService)
		{
			_authService = authService;
		}

		[HttpPost("login")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> Login([FromBody] LoginRequest request)
		{
			var result = await _authService.LoginAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("register")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> Register([FromBody] RegisterRequest request)
		{
			var result = await _authService.RegisterAsync(request, null);
			return HandleResponse(result);
		}

		[HttpPost("admin/register")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> RegisterByAdmin([FromBody] RegisterRequest request, [FromQuery] UserRole role)
		{
			var result = await _authService.RegisterAsync(request, role);
			return HandleResponse(result);
		}

		[HttpGet("verify-email")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> VerifyEmail([FromQuery] VerifyEmailRequest request)
		{
			var result = await _authService.VerifyEmailAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("google-login")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
		{
			var result = await _authService.LoginWithGoogleAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("api-token")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<TokenResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<TokenResponse>>> CreateApiToken()
		{
			var userId = GetCurrentUserId();

			var tokenResult = await _authService.CreateApiTokenAsync(userId);
			return HandleResponse(tokenResult);
		}

		[HttpPost("forgot-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
		{
			var result = await _authService.ForgotPasswordAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("reset-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ResetPassword([FromBody] ResetPasswordRequest request)
		{
			var result = await _authService.ResetPasswordAsync(request);
			return HandleResponse(result);
		}

	}
}
