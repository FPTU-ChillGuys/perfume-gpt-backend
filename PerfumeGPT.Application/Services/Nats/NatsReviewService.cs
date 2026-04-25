using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Review operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsReviewService : INatsReviewService
{
	private readonly INatsReviewRepository _reviewRepository;

	public NatsReviewService(INatsReviewRepository reviewRepository)
	{
		_reviewRepository = reviewRepository;
	}

	public async Task<NatsReviewPagedResponse> GetPagedReviewsAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		Guid? userId = null,
		int? minRating = null,
		int? maxRating = null,
		bool? hasImages = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var (items, totalCount) = await _reviewRepository.GetPagedReviewsForNatsAsync(
			pageNumber,
			pageSize,
			variantId,
			userId,
			minRating,
			maxRating,
			hasImages,
			sortBy,
			isDescending);

		return new NatsReviewPagedResponse
		{
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize,
			TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
			Items = items
		};
	}

	public async Task<List<NatsReviewListItemResponse>> GetVariantReviewsAsync(Guid variantId)
	{
		return await _reviewRepository.GetReviewsByVariantIdForNatsAsync(variantId);
	}

	public async Task<NatsReviewVariantStats> GetVariantStatsAsync(Guid variantId)
	{
		return await _reviewRepository.GetVariantReviewStatisticsForNatsAsync(variantId);
	}
}
