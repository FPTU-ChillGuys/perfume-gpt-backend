using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IReviewRepository : IGenericRepository<Review>
	{
		/// <summary>
		/// Gets a review with all related data (User, OrderDetail, Variant, Product, Images).
		/// </summary>
		Task<Review?> GetReviewWithDetailsAsync(Guid reviewId);

		/// <summary>
		/// Gets paged reviews with filtering and includes related data.
		/// </summary>
		Task<(List<Review> Items, int TotalCount)> GetPagedReviewsAsync(GetPagedReviewsRequest request);

		/// <summary>
		/// Checks if a user can review a specific order detail.
		/// User must have received the order and not already reviewed it.
		/// </summary>
		Task<bool> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId);

		/// <summary>
		/// Checks if a user has already reviewed a specific order detail.
		/// </summary>
		Task<bool> HasUserReviewedOrderDetailAsync(Guid userId, Guid orderDetailId);

		/// <summary>
		/// Gets all reviews for a specific variant with optional status filter.
		/// </summary>
		Task<List<Review>> GetReviewsByVariantIdAsync(Guid variantId, ReviewStatus? status = null);

		/// <summary>
		/// Gets all reviews by a specific user.
		/// </summary>
		Task<List<Review>> GetReviewsByUserIdAsync(Guid userId);

		/// <summary>
		/// Calculates review statistics for a specific variant.
		/// </summary>
		Task<(int TotalReviews, double AverageRating, int[] StarCounts)> GetVariantReviewStatisticsAsync(Guid variantId);

		/// <summary>
		/// Gets reviews that are pending moderation.
		/// </summary>
		Task<(List<Review> Items, int TotalCount)> GetPendingReviewsAsync(int pageNumber, int pageSize);

		/// <summary>
		/// Verifies that an order detail belongs to a user and the order is delivered.
		/// </summary>
		Task<bool> IsOrderDetailDeliveredToUserAsync(Guid userId, Guid orderDetailId);
	}
}
