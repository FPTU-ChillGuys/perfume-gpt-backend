using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IReviewRepository : IGenericRepository<Review>
	{
		Task<ReviewDetailResponse?> GetReviewWithDetailsAsync(Guid reviewId);
		Task<(List<ReviewListItem> Items, int TotalCount)> GetPagedReviewsAsync(GetPagedReviewsRequest request);
		Task<List<ReviewResponse>> GetReviewsByVariantIdAsync(Guid variantId);
		Task<List<ReviewResponse>> GetReviewsByUserIdAsync(Guid userId);

		/// <summary>
		/// Calculates review statistics for a specific variant.
		/// </summary>
		Task<(int TotalReviews, double AverageRating, int[] StarCounts)> GetVariantReviewStatisticsAsync(Guid variantId);

		Task<bool> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId);
		Task<bool> HasUserReviewedOrderDetailAsync(Guid userId, Guid orderDetailId);

	}
}
