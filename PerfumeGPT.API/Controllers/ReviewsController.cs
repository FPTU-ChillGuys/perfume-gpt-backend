using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ReviewsController : BaseApiController
	{
		private readonly IReviewService _reviewService;
		private readonly IMediaService _mediaService;

		public ReviewsController(IReviewService reviewService, IMediaService mediaService)
		{
			_reviewService = reviewService;
			_mediaService = mediaService;
		}

		#region USER ENDPOINTS

		[HttpGet("me")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<ReviewResponse>>>> GetMyReviews()
		{
			var userId = GetCurrentUserId();
			var response = await _reviewService.GetUserReviewsAsync(userId);
			return HandleResponse(response);
		}

		[HttpGet("can-review/{orderDetailId:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> CanReviewOrderDetail(Guid orderDetailId)
		{
			var userId = GetCurrentUserId();
			var response = await _reviewService.CanUserReviewOrderDetailAsync(userId, orderDetailId);
			return HandleResponse(response);
		}

		[HttpPost]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<Guid>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<Guid>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<Guid>>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<Guid>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<Guid>>>> CreateReview([FromBody] CreateReviewRequest request)
		{
			var validation = ValidateRequestBody<CreateReviewRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _reviewService.CreateReviewAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpPut("{reviewId:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<string>>>> UpdateReview(Guid reviewId, [FromBody] UpdateReviewRequest request)
		{
			var validation = ValidateRequestBody<UpdateReviewRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _reviewService.UpdateReviewAsync(userId, reviewId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{reviewId:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteReview(Guid reviewId)
		{
			var userId = GetCurrentUserId();
			var response = await _reviewService.DeleteReviewAsync(userId, reviewId);
			return HandleResponse(response);
		}

		#endregion

		#region STAFF ENDPOINTS

		[HttpPost("{reviewId:guid}/moderate")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> ModerateReview(Guid reviewId, [FromBody] ModerateReviewRequest request)
		{
			var validation = ValidateRequestBody<ModerateReviewRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _reviewService.ModerateReviewAsync(staffId, reviewId, request);
			return HandleResponse(response);
		}

		[HttpGet("pending")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ReviewListItem>>>> GetPendingReviews(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 10)
		{
			var response = await _reviewService.GetPendingReviewsAsync(pageNumber, pageSize);
			return HandleResponse(response);
		}

		#endregion

		#region PUBLIC ENDPOINTS

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ReviewListItem>>>> GetReviews([FromQuery] GetPagedReviewsRequest request)
		{
			var response = await _reviewService.GetReviewsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{reviewId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ReviewDetailResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ReviewDetailResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ReviewDetailResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ReviewDetailResponse>>> GetReviewById(Guid reviewId)
		{
			var response = await _reviewService.GetReviewByIdAsync(reviewId);
			return HandleResponse(response);
		}

		[HttpGet("variant/{variantId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<ReviewResponse>>>> GetVariantReviews(Guid variantId)
		{
			var response = await _reviewService.GetVariantReviewsAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("variant/{variantId:guid}/statistics")]
		[ProducesResponseType(typeof(BaseResponse<ReviewStatisticsResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ReviewStatisticsResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ReviewStatisticsResponse>>> GetVariantStatistics(Guid variantId)
		{
			var response = await _reviewService.GetVariantReviewStatisticsAsync(variantId);
			return HandleResponse(response);
		}

		#endregion

		#region MEDIA ENDPOINTS
		[HttpPost("images/temporary")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages(
			[FromForm] ReviewUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{reviewId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetReviewImages(Guid reviewId)
		{
			var response = await _reviewService.GetReviewImagesAsync(reviewId);
			return HandleResponse(response);
		}
		#endregion
	}
}
