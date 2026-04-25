using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Sales operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsSalesService
{
	/// <summary>
	/// Get sales analytics by variant ID for AI analysis
	/// </summary>
	Task<NatsSalesAnalyticsResponse?> GetSalesAnalyticsByVariantIdAsync(Guid variantId);
}
