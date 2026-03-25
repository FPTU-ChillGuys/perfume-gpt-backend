using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ReviewService : IReviewService
	{
		#region Dependencies
		private readonly IMediaService _mediaService;
		private readonly IReviewRepository _reviewRepository;
		private readonly IValidator<CreateReviewRequest> _createValidator;
		private readonly IValidator<AnswerReviewRequest> _answerValidator;
		private readonly MediaBulkActionHelper _helper;

		public ReviewService(
			IMediaService mediaService,
			IValidator<CreateReviewRequest> createValidator,
			IValidator<AnswerReviewRequest> answerValidator,
			MediaBulkActionHelper helper,
			IReviewRepository reviewRepository)
		{
			_mediaService = mediaService;
			_createValidator = createValidator;
			_answerValidator = answerValidator;
			_helper = helper;
			_reviewRepository = reviewRepository;
		}
		#endregion Dependencies

		public async Task<BaseResponse<BulkActionResult<Guid>>> CreateReviewAsync(Guid userId, CreateReviewRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				throw AppException.BadRequest("Validation failed", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);
			}

			var canReview = await _reviewRepository.CanUserReviewOrderDetailAsync(userId, request.OrderDetailId);
			if (!canReview)
			{
				throw AppException.BadRequest("You cannot review this item. Either you haven't purchased it, the order is not delivered yet, or you've already reviewed it.");
			}

			var review = Review.Create(userId, request.OrderDetailId, request.Rating, request.Comment);

			await _reviewRepository.AddAsync(review);
			var saved = await _reviewRepository.SaveChangesAsync();

			if (!saved)
				throw AppException.Internal("Failed to create review");

			var metadata = new BulkActionMetadata();
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				var conversionResult = await _helper.ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, EntityType.Review, review.Id);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			var result = new BulkActionResult<Guid>(review.Id, metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Review submitted successfully with {metadata.TotalFailed} media upload failure(s)."
				: "Review submitted successfully.";

			return BaseResponse<BulkActionResult<Guid>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId, bool canDeleteAny = false)
		{
			var review = await _reviewRepository.GetByIdAsync(reviewId) ?? throw AppException.NotFound("Review not found");

			if (!canDeleteAny && !review.IsAuthor(userId))
				throw AppException.Forbidden("You are not authorized to delete this review");

			_reviewRepository.Remove(review);

			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Review, reviewId);

			var saved = await _reviewRepository.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to delete review");

			return BaseResponse<string>.Ok("Review deleted successfully");
		}

		public async Task<BaseResponse<string>> AnswerReviewAsync(Guid reviewId, Guid staffId, AnswerReviewRequest request)
		{
			var validationResult = await _answerValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				throw AppException.BadRequest("Validation failed", [.. validationResult.Errors.Select(e => e.ErrorMessage)]);
			}

			var review = await _reviewRepository.GetByIdAsync(reviewId) ?? throw AppException.NotFound("Review not found");

			if (review.HasStaffResponse())
				throw AppException.BadRequest("This review already has a staff response.");

			review.AnswerByStaff(staffId, request.StaffFeedbackComment, DateTime.UtcNow);

			var saved = await _reviewRepository.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to answer review");

			return BaseResponse<string>.Ok("Review answered successfully");
		}

		public async Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request)
		{
			var (items, totalCount) = await _reviewRepository.GetPagedReviewsAsync(request);

			var pagedResult = new PagedResult<ReviewListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ReviewListItem>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<ReviewDetailResponse>> GetReviewByIdAsync(Guid reviewId)
		{
			var response = await _reviewRepository.GetReviewWithDetailsAsync(reviewId) ?? throw AppException.NotFound("Review not found");
			return BaseResponse<ReviewDetailResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId)
		{
			var response = await _reviewRepository.GetReviewsByUserIdAsync(userId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId)
		{
			var response = await _reviewRepository.GetReviewsByVariantIdAsync(variantId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<ReviewStatisticsResponse>> GetVariantReviewStatisticsAsync(Guid variantId)
		{
			var (totalReviews, averageRating, starCounts) = await _reviewRepository.GetVariantReviewStatisticsAsync(variantId);

			var response = new ReviewStatisticsResponse
			{
				VariantId = variantId,
				TotalReviews = totalReviews,
				AverageRating = averageRating,
				FiveStarCount = starCounts[4],
				FourStarCount = starCounts[3],
				ThreeStarCount = starCounts[2],
				TwoStarCount = starCounts[1],
				OneStarCount = starCounts[0]
			};

			return BaseResponse<ReviewStatisticsResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<MediaResponse>>> GetReviewImagesAsync(Guid reviewId)
			=> await _mediaService.GetMediaByEntityAsync(EntityType.Review, reviewId);
	}
}
