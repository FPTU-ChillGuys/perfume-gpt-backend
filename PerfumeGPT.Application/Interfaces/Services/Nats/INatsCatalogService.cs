using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Sourcing Catalog operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsCatalogService
{
	/// <summary>
	/// Get catalogs by variant ID for AI analysis
	/// </summary>
	Task<NatsCatalogResponse> GetCatalogsByVariantIdAsync(Guid variantId);
}
