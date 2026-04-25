using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Sales operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsSalesRepository
{
	/// <summary>
	/// Get sales analytics by variant ID for AI analysis
	/// </summary>
	Task<NatsSalesAnalyticsResponse?> GetSalesAnalyticsByVariantIdForNatsAsync(Guid variantId);
}
