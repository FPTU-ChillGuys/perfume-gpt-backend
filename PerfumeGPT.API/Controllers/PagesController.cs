using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Pages;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Pages;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PagesController : BaseApiController
	{
		private readonly IPageService _pageService;
		private readonly IMediaService _mediaService;

		public PagesController(IPageService pageService, IMediaService mediaService)
		{
			_pageService = pageService;
			_mediaService = mediaService;
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<PageResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<PageResponse>>>> GetPages([FromQuery] GetPagedPageRequest request)
		{
			var response = await _pageService.GetPagesAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{slug}")]
		[ProducesResponseType(typeof(BaseResponse<PageResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PageResponse>>> GetPageContent(string slug)
		{
			var validationResult = ValidateRequiredString(slug, "Slug");
			if (validationResult != null) return validationResult;

			var response = await _pageService.GetPageContentAsync(slug);
			return HandleResponse(response);
		}

		[HttpPost]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PageResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PageResponse>>> CreatePage([FromBody] CreatePageRequest request)
		{
			var response = await _pageService.CreatePageAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{slug}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PageResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PageResponse>>> UpdatePage([FromRoute] string slug, [FromBody] UpdatePageRequest request)
		{
			var validationResult = ValidateRequiredString(slug, "Slug");
			if (validationResult != null) return validationResult;

			var response = await _pageService.UpdatePageAsync(slug, request);
			return HandleResponse(response);
		}

		[HttpDelete("{slug}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse>> DeletePage([FromRoute] string slug)
		{
			var validationResult = ValidateRequiredString(slug, "Slug");
			if (validationResult != null) return validationResult;

			var response = await _pageService.DeletePageAsync(slug);
			return HandleResponse(response);
		}

		[HttpPost("{slug}/publish")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PageResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PageResponse>>> PublishPage([FromRoute] string slug)
		{
			var response = await _pageService.PublishPageAsync(slug);
			return HandleResponse(response);
		}

		[HttpPost("images/temporary")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages([FromForm] PageUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadPageTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}
	}
}
