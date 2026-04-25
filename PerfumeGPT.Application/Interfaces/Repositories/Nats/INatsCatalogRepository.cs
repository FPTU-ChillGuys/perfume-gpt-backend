using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Sourcing Catalog operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsCatalogRepository
{
	/// <summary>
	/// Get catalogs by variant ID for AI analysis
	/// </summary>
	Task<List<NatsCatalogItemResponse>> GetCatalogsByVariantIdForNatsAsync(Guid variantId);
}
