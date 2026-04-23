using FluentValidation;
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
		private readonly IValidator<CreateReviewRequest> _createValidator;
		private readonly IValidator<AnswerReviewRequest> _answerValidator;

		public ReviewsController(
			IReviewService reviewService,
			IMediaService mediaService,
			IValidator<CreateReviewRequest> createValidator,
			IValidator<AnswerReviewRequest> answerValidator)
		{
			_reviewService = reviewService;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_answerValidator = answerValidator;
		}

		#region USER ENDPOINTS
		[HttpGet("me")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ReviewResponse>>>> GetMyReviews()
		{
			var userId = GetCurrentUserId();
			var response = await _reviewService.GetUserReviewsAsync(userId);
			return HandleResponse(response);
		}

		[HttpPost]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<Guid>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<Guid>>>> CreateReview([FromBody] CreateReviewRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _reviewService.CreateReviewAsync(userId, request);
			return HandleResponse(response);
		}
		#endregion USER ENDPOINTS



		#region STAFF ENDPOINTS
		[HttpPost("{reviewId:guid}/answer")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> AnswerReview([FromRoute] Guid reviewId, [FromBody] AnswerReviewRequest request)
		{
			var validation = await ValidateRequestAsync(_answerValidator, request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _reviewService.AnswerReviewAsync(reviewId, staffId, request);
			return HandleResponse(response);
		}
		#endregion STAFF ENDPOINTS



		#region PUBLIC ENDPOINTS
		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ReviewListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ReviewListItem>>>> GetReviews([FromQuery] GetPagedReviewsRequest request)
		{
			var response = await _reviewService.GetReviewsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{reviewId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ReviewDetailResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ReviewDetailResponse>>> GetReviewById([FromRoute] Guid reviewId)
		{
			var response = await _reviewService.GetReviewByIdAsync(reviewId);
			return HandleResponse(response);
		}

		[HttpGet("variant/{variantId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<List<ReviewResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ReviewResponse>>>> GetVariantReviews([FromRoute] Guid variantId)
		{
			var response = await _reviewService.GetVariantReviewsAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("variant/{variantId:guid}/statistics")]
		[ProducesResponseType(typeof(BaseResponse<ReviewStatisticsResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ReviewStatisticsResponse>>> GetVariantStatistics([FromRoute] Guid variantId)
		{
			var response = await _reviewService.GetVariantReviewStatisticsAsync(variantId);
			return HandleResponse(response);
		}

		[HttpDelete("{reviewId:guid}")]
		[Authorize(Roles = "user,staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteReview([FromRoute] Guid reviewId)
		{
			var userId = GetCurrentUserId();
			var canDeleteAny = User.IsInRole("staff") || User.IsInRole("admin");
			var response = await _reviewService.DeleteReviewAsync(userId, reviewId, canDeleteAny);
			return HandleResponse(response);
		}
		#endregion PUBLIC ENDPOINTS



		#region MEDIA ENDPOINTS
		[HttpPost("images/temporary")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages([FromForm] ReviewUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadReviewTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{reviewId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetReviewImages([FromRoute] Guid reviewId)
		{
			var response = await _reviewService.GetReviewImagesAsync(reviewId);
			return HandleResponse(response);
		}
		#endregion MEDIA ENDPOINTS
	}
}
