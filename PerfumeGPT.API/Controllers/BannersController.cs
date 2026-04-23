using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BannersController : BaseApiController
	{
		private readonly IBannerService _bannerService;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateBannerRequest> _createValidator;
		private readonly IValidator<UpdateBannerRequest> _updateValidator;

		public BannersController(
			IBannerService bannerService,
			IMediaService mediaService,
			IValidator<CreateBannerRequest> createValidator,
			IValidator<UpdateBannerRequest> updateValidator)
		{
			_bannerService = bannerService;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("home")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(BaseResponse<List<BannerResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<BannerResponse>>>> GetVisibleBanners([FromQuery] BannerPosition? position)
		{
			var response = await _bannerService.GetVisibleBannersAsync(position);
			return HandleResponse(response);
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BannerResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<BannerResponse>>>> GetPagedBanners([FromQuery] GetPagedBannersRequest request)
		{
			var response = await _bannerService.GetPagedBannersAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{bannerId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<BannerResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BannerResponse>>> GetBannerById([FromRoute] Guid bannerId)
		{
			var response = await _bannerService.GetBannerByIdAsync(bannerId);
			return HandleResponse(response);
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateBanner([FromBody] CreateBannerRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var response = await _bannerService.CreateBannerAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{bannerId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateBanner([FromRoute] Guid bannerId, [FromBody] UpdateBannerRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var response = await _bannerService.UpdateBannerAsync(bannerId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{bannerId:guid}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteBanner([FromRoute] Guid bannerId)
		{
			var response = await _bannerService.DeleteBannerAsync(bannerId);
			return HandleResponse(response);
		}

		[HttpPost("images/temporary")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages([FromForm] BannerUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadBannerTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}
	}
}
