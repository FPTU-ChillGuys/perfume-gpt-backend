using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ReviewService : IReviewService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMediaService _mediaService;
		private readonly IValidator<CreateReviewRequest> _createValidator;
		private readonly IValidator<UpdateReviewRequest> _updateValidator;
		private readonly IValidator<ModerateReviewRequest> _moderateValidator;
		private readonly IValidator<GetPagedReviewsRequest> _getPagedValidator;
		private readonly IMapper _mapper;

		public ReviewService(
			IUnitOfWork unitOfWork,
			IMediaService mediaService,
			IValidator<CreateReviewRequest> createValidator,
			IValidator<UpdateReviewRequest> updateValidator,
			IValidator<ModerateReviewRequest> moderateValidator,
			IValidator<GetPagedReviewsRequest> getPagedValidator,
			IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mediaService = mediaService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_moderateValidator = moderateValidator;
			_getPagedValidator = getPagedValidator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<BulkActionResult<Guid>>> CreateReviewAsync(Guid userId, CreateReviewRequest request)
		{
			// Validate request
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<Guid>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			// Check if user can review this order detail
			var canReview = await _unitOfWork.Reviews.CanUserReviewOrderDetailAsync(userId, request.OrderDetailId);
			if (!canReview)
			{
				return BaseResponse<BulkActionResult<Guid>>.Fail(
					"You cannot review this item. Either you haven't purchased it, the order is not delivered yet, or you've already reviewed it.",
					ResponseErrorType.BadRequest
				);
			}

			// Create review entity
			var review = _mapper.Map<Review>(request);
			review.UserId = userId;
			review.Status = ReviewStatus.Pending;

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
				? $"Review submitted successfully with {metadata.TotalFailed} media upload failure(s). It will be published after moderation."
				: "Review submitted successfully. It will be published after moderation.";

			return BaseResponse<BulkActionResult<Guid>>.Ok(result, message);
		}

		public async Task<BaseResponse<BulkActionResult<string>>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<BulkActionResult<string>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]
				);
			}

			// Get review
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
			if (review == null || review.IsDeleted)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			// Check ownership
			if (review.UserId != userId)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("You can only edit your own reviews", ResponseErrorType.Forbidden);
			}

			// === IMAGE MANAGEMENT ===
			var metadata = new BulkActionMetadata();

			// Delete specified images first
			if (request.MediaIdsToDelete != null && request.MediaIdsToDelete.Count != 0)
			{
				var deleteResult = await DeleteMultipleMediaAsync(request.MediaIdsToDelete);
				if (deleteResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Deletion", deleteResult));
				}
			}

			// Add new images from temporary media
			if (request.TemporaryMediaIdsToAdd != null && request.TemporaryMediaIdsToAdd.Any())
			{
				var conversionResult = await ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIdsToAdd, reviewId);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			// === UPDATE REVIEW DATA ===
			review.Rating = request.Rating;
			review.Comment = request.Comment;
			review.Status = ReviewStatus.Pending; // Reset to pending after edit

			_unitOfWork.Reviews.Update(review);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
			{
				return BaseResponse<BulkActionResult<string>>.Fail("Failed to update review", ResponseErrorType.InternalError);
			}

			var result = new BulkActionResult<string>("Review updated successfully", metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
				? $"Review updated with {metadata.TotalFailed} media operation failure(s). It will be re-moderated."
				: "Review updated successfully. It will be re-moderated.";

			return BaseResponse<BulkActionResult<string>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId)
		{
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
			if (review == null || review.IsDeleted)
			{
				return BaseResponse<string>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			// Check ownership
			if (review.UserId != userId)
			{
				return BaseResponse<string>.Fail("You can only delete your own reviews", ResponseErrorType.Forbidden);
			}

			// Soft delete
			_unitOfWork.Reviews.Remove(review);

			// Delete associated images
			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Review, reviewId);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to delete review", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok("Review deleted successfully");
		}

		public async Task<BaseResponse<string>> ModerateReviewAsync(Guid staffId, Guid reviewId, ModerateReviewRequest request)
		{
			// Validate request
			var validationResult = await _moderateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<string>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			// Get review
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
			if (review == null || review.IsDeleted)
			{
				return BaseResponse<string>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			// Update moderation fields
			review.Status = request.Status;
			review.ModeratedByStaffId = staffId;
			review.ModeratedAt = DateTime.UtcNow;
			review.ModerationReason = request.ModerationReason;

			_unitOfWork.Reviews.Update(review);
			var saved = await _unitOfWork.SaveChangesAsync();

			if (!saved)
			{
				return BaseResponse<string>.Fail("Failed to moderate review", ResponseErrorType.InternalError);
			}

			var message = request.Status == ReviewStatus.Approved
				? "Review approved successfully"
				: "Review rejected successfully";

			return BaseResponse<string>.Ok(message);
		}

		public async Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request)
		{
			// Validate request
			var validationResult = await _getPagedValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				return BaseResponse<PagedResult<ReviewListItem>>.Fail(
					"Validation failed",
					ResponseErrorType.BadRequest,
					validationResult.Errors.Select(e => e.ErrorMessage).ToList()
				);
			}

			var (items, totalCount) = await _unitOfWork.Reviews.GetPagedReviewsAsync(request);

			var reviewListItems = _mapper.Map<List<ReviewListItem>>(items);

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
			var review = await _unitOfWork.Reviews.GetReviewWithDetailsAsync(reviewId);
			if (review == null)
			{
				return BaseResponse<ReviewDetailResponse>.Fail("Review not found", ResponseErrorType.NotFound);
			}

			var response = _mapper.Map<ReviewDetailResponse>(review);
			return BaseResponse<ReviewDetailResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId)
		{
			var reviews = await _unitOfWork.Reviews.GetReviewsByUserIdAsync(userId);
			var response = _mapper.Map<List<ReviewResponse>>(reviews);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId)
		{
			var reviews = await _unitOfWork.Reviews.GetReviewsByVariantIdAsync(variantId, ReviewStatus.Approved);
			var response = _mapper.Map<List<ReviewResponse>>(reviews);
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

		public async Task<BaseResponse<PagedResult<ReviewListItem>>> GetPendingReviewsAsync(int pageNumber = 1, int pageSize = 10)
		{
			var (items, totalCount) = await _unitOfWork.Reviews.GetPendingReviewsAsync(pageNumber, pageSize);

			var reviewListItems = _mapper.Map<List<ReviewListItem>>(items);

			var pagedResult = new PagedResult<ReviewListItem>(
				reviewListItems,
				pageNumber,
				pageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ReviewListItem>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<bool>> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId)
		{
			var canReview = await _unitOfWork.Reviews.CanUserReviewOrderDetailAsync(userId, orderDetailId);
			return BaseResponse<bool>.Ok(canReview);
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

		private async Task<BulkActionResponse> ConvertTemporaryMediaToPermanentAsync(List<Guid> temporaryMediaIds, Guid reviewId)
		{
			var response = new BulkActionResponse();

			foreach (var tempMediaId in temporaryMediaIds)
			{
				try
				{
					// Get temporary media
					var tempMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(tempMediaId);
					if (tempMedia == null)
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = tempMediaId,
							ErrorMessage = "Temporary media not found"
						});
						continue;
					}

					if (tempMedia.IsExpired)
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = tempMediaId,
							ErrorMessage = "Temporary media has expired"
						});
						continue;
					}

					// Create permanent media from temporary
					var media = new Media
					{
						Url = tempMedia.Url,
						AltText = tempMedia.AltText,
						EntityType = EntityType.Review,
						ReviewId = reviewId,
						DisplayOrder = tempMedia.DisplayOrder,
						IsPrimary = false,
						PublicId = tempMedia.PublicId,
						FileSize = tempMedia.FileSize,
						MimeType = tempMedia.MimeType
					};

					await _unitOfWork.Media.AddAsync(media);
					_unitOfWork.TemporaryMedia.Remove(tempMedia);

					response.SucceededIds.Add(tempMediaId);
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = tempMediaId,
						ErrorMessage = $"Failed to convert media: {ex.Message}"
					});
				}
			}

			if (response.SucceededIds.Count > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			return response;
		}

		private async Task<BulkActionResponse> DeleteMultipleMediaAsync(List<Guid> mediaIds)
		{
			var response = new BulkActionResponse();

			foreach (var mediaId in mediaIds)
			{
				try
				{
					var deleteResult = await _mediaService.DeleteMediaAsync(mediaId);
					if (deleteResult.Success)
					{
						response.SucceededIds.Add(mediaId);
					}
					else
					{
						response.FailedItems.Add(new BulkActionError
						{
							Id = mediaId,
							ErrorMessage = deleteResult.Message ?? "Unknown error"
						});
					}
				}
				catch (Exception ex)
				{
					response.FailedItems.Add(new BulkActionError
					{
						Id = mediaId,
						ErrorMessage = $"Exception during deletion: {ex.Message}"
					});
				}
			}

			return response;
		}

		#endregion
	}
}
