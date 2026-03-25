using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ReviewService : IReviewService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateReviewRequest> _createValidator;
		private readonly IValidator<UpdateReviewRequest> _updateValidator;
		private readonly IMapper _mapper;
		private readonly MediaBulkActionHelper _helper;

		public ReviewService(
			IUnitOfWork unitOfWork,
			IMediaService mediaService,
			IValidator<CreateReviewRequest> createValidator,
			IValidator<UpdateReviewRequest> updateValidator,
			IMapper mapper,
			MediaBulkActionHelper helper)
		{
			_unitOfWork = unitOfWork;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
			_helper = helper;
		}

		#endregion Dependencies

		public async Task<BaseResponse<BulkActionResult<Guid>>> CreateReviewAsync(Guid userId, CreateReviewRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<Guid>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			var canReview = await _unitOfWork.Reviews.CanUserReviewOrderDetailAsync(userId, request.OrderDetailId);
			if (!canReview)
			{
				return BaseResponse<BulkActionResult<Guid>>.Fail(
					"You cannot review this item. Either you haven't purchased it, the order is not delivered yet, or you've already reviewed it.",
					ResponseErrorType.BadRequest
				);
			}

			var review = _mapper.Map<Review>(request);
			review.UserId = userId;

			await _unitOfWork.Reviews.AddAsync(review);
			var saved = await _unitOfWork.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<BulkActionResult<Guid>>.Fail("Failed to create review", ResponseErrorType.InternalError);
			}

			var metadata = new BulkActionMetadata();
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, review.Id);
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

		public async Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId)
		{
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
			if (review == null || review.IsDeleted)
			{
				return BaseResponse<string>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			if (review.UserId != userId)
			{
				return BaseResponse<string>.Fail("You can only delete your own reviews", ResponseErrorType.Forbidden);
			}

			_unitOfWork.Reviews.Remove(review);

			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Review, reviewId);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete review", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok("Review deleted successfully");
		}

		public async Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Reviews.GetPagedReviewsAsync(request);

			var reviewListItems = items;

			var pagedResult = new PagedResult<ReviewListItem>(
				reviewListItems,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ReviewListItem>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<ReviewDetailResponse>> GetReviewByIdAsync(Guid reviewId)
		{
			var response = await _unitOfWork.Reviews.GetReviewWithDetailsAsync(reviewId);
			if (response == null)
			{
				return BaseResponse<ReviewDetailResponse>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			return BaseResponse<ReviewDetailResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId)
		{
			var response = await _unitOfWork.Reviews.GetReviewsByUserIdAsync(userId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId)
		{
			var response = await _unitOfWork.Reviews.GetReviewsByVariantIdAsync(variantId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<ReviewStatisticsResponse>> GetVariantReviewStatisticsAsync(Guid variantId)
		{
			var (totalReviews, averageRating, starCounts) = await _unitOfWork.Reviews.GetVariantReviewStatisticsAsync(variantId);

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
		{
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
			if (review == null || review.IsDeleted)
			{
				return BaseResponse<List<MediaResponse>>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			var response = await _mediaService.GetMediaByEntityAsync(EntityType.Review, reviewId);
			return response;
		}

		#region Private Methods

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(
			List<Guid> temporaryMediaIds,
			Guid reviewId)
		{
			return await _helper.ConvertTemporaryMediaToPermanentAsync(
				temporaryMediaIds,
				EntityType.Review,
				reviewId);
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			return await _helper.DeleteMultipleMediaAsync(mediaIds);
		}

		#endregion
	}
}
