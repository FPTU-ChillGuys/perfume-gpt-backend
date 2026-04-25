using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Product operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsProductRepository
{
	/// <summary>
	/// Get products by IDs for AI analysis
	/// </summary>
	Task<List<NatsProductResponse>> GetProductsByIdsForNatsAsync(IEnumerable<Guid> productIds);

	/// <summary>
	/// Get product by ID for AI analysis
	/// </summary>
	Task<NatsProductResponse?> GetProductByIdForNatsAsync(Guid productId);
}
