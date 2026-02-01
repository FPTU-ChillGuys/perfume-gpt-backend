using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IReviewService
	{
		/// <summary>
		/// Creates a new review for a purchased product variant.
		/// Validates that the user purchased the item and hasn't already reviewed it.
		/// </summary>
		Task<BaseResponse<Guid>> CreateReviewAsync(Guid userId, CreateReviewRequest request);

		/// <summary>
		/// Updates an existing review. Only the review owner can update.
		/// </summary>
		Task<BaseResponse<string>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewRequest request);

		/// <summary>
		/// Soft deletes a review. Only the review owner can delete.
		/// </summary>
		Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId);

		/// <summary>
		/// Moderates a review (Approve/Reject). Only staff can moderate.
		/// </summary>
		Task<BaseResponse<string>> ModerateReviewAsync(Guid staffId, Guid reviewId, ModerateReviewRequest request);

		/// <summary>
		/// Gets paginated reviews with filtering.
		/// </summary>
		Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request);

		/// <summary>
		/// Gets a single review with full details.
		/// </summary>
		Task<BaseResponse<ReviewDetailResponse>> GetReviewByIdAsync(Guid reviewId);

		/// <summary>
		/// Gets all reviews by a specific user.
		/// </summary>
		Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId);

		/// <summary>
		/// Gets all approved reviews for a specific variant.
		/// </summary>
		Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId);

		/// <summary>
		/// Gets review statistics for a specific variant.
		/// </summary>
		Task<BaseResponse<ReviewStatisticsResponse>> GetVariantReviewStatisticsAsync(Guid variantId);

		/// <summary>
		/// Gets pending reviews for staff moderation.
		/// </summary>
		Task<BaseResponse<PagedResult<ReviewListItem>>> GetPendingReviewsAsync(int pageNumber = 1, int pageSize = 10);

		/// <summary>
		/// Checks if a user can review a specific order detail.
		/// </summary>
		Task<BaseResponse<bool>> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId);

		/// <summary>
		/// Gets all images for a specific review (for editing purposes).
		/// </summary>
		Task<BaseResponse<List<MediaResponse>>> GetReviewImagesAsync(Guid reviewId);
	}
}
