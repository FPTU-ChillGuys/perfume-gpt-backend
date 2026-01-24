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
		[Authorize(Roles = "Admin")]
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
	}
}
