using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Review operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsReviewService
{
	/// <summary>
	/// Get paged reviews for AI analysis
	/// </summary>
	Task<NatsReviewPagedResponse> GetPagedReviewsAsync(
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
	Task<List<NatsReviewListItemResponse>> GetVariantReviewsAsync(Guid variantId);

	/// <summary>
	/// Get review statistics by variant ID
	/// </summary>
	Task<NatsReviewVariantStats> GetVariantStatsAsync(Guid variantId);
}
