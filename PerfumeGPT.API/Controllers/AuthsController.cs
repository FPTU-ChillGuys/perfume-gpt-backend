using FluentValidation;
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
		private readonly IValidator<RegisterRequest> _registerValidator;
		private readonly IValidator<LoginRequest> _loginValidator;
		private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
		private readonly IValidator<GoogleLoginRequest> _googleLoginValidator;
		private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
		private readonly IValidator<VerifyEmailRequest> _verifyEmailValidator;

		public AuthsController(
			IAuthService authService,
			IValidator<RegisterRequest> registerValidator,
			IValidator<LoginRequest> loginValidator,
			IValidator<ResetPasswordRequest> resetPasswordValidator,
			IValidator<GoogleLoginRequest> googleLoginValidator,
			IValidator<ForgotPasswordRequest> forgotPasswordValidator,
			IValidator<VerifyEmailRequest> verifyEmailValidator)
		{
			_authService = authService;
			_registerValidator = registerValidator;
			_loginValidator = loginValidator;
			_resetPasswordValidator = resetPasswordValidator;
			_googleLoginValidator = googleLoginValidator;
			_forgotPasswordValidator = forgotPasswordValidator;
			_verifyEmailValidator = verifyEmailValidator;
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
			var validation = await ValidateRequestAsync(_loginValidator, request);
			if (validation != null) return validation;

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
			var validation = await ValidateRequestAsync(_registerValidator, request);
			if (validation != null) return validation;

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
			var validation = await ValidateRequestAsync(_registerValidator, request);
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
		public async Task<ActionResult<BaseResponse<string>>> VerifyEmail([FromQuery] VerifyEmailRequest request)
		{
			var validation = await ValidateRequestAsync(_verifyEmailValidator, request);
			if (validation != null) return validation;

			var result = await _authService.VerifyEmailAsync(request);
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
			var validation = await ValidateRequestAsync(_googleLoginValidator, request);
			if (validation != null) return validation;

			var result = await _authService.LoginWithGoogleAsync(request);
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

			var tokenResult = await _authService.CreateApiTokenAsync(userId);
			return HandleResponse(tokenResult);
		}

		[HttpPost("forgot-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
		{
			var validation = await ValidateRequestAsync(_forgotPasswordValidator, request);
			if (validation != null) return validation;

			var result = await _authService.ForgotPasswordAsync(request);
			return HandleResponse(result);
		}

		[HttpPost("reset-password")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> ResetPassword([FromBody] ResetPasswordRequest request)
		{
			var validation = await ValidateRequestAsync(_resetPasswordValidator, request);
			if (validation != null) return validation;

			var result = await _authService.ResetPasswordAsync(request);
			return HandleResponse(result);
		}

	}
}
