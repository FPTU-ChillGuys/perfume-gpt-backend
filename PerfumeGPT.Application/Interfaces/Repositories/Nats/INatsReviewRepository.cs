using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Review operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsReviewRepository
{
	/// <summary>
	/// Get paged reviews with full details for AI analysis
	/// </summary>
	Task<(List<NatsReviewListItemResponse> Items, int TotalCount)> GetPagedReviewsForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		Guid? userId = null,
		int? minRating = null,
		int? maxRating = null,
		bool? hasImages = null,
		string? sortBy = null,
		bool isDescending = false);

	/// <summary>
	/// Get reviews by variant ID for AI analysis
	/// </summary>
	Task<List<NatsReviewListItemResponse>> GetReviewsByVariantIdForNatsAsync(Guid variantId);

	/// <summary>
	/// Get review statistics by variant ID
	/// </summary>
	Task<NatsReviewVariantStats> GetVariantReviewStatisticsForNatsAsync(Guid variantId);
}
