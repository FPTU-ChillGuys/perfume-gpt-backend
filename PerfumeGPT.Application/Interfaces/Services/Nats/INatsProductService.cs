using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Product operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsProductService
{
	/// <summary>
	/// Get products by IDs for AI analysis
	/// </summary>
	Task<NatsProductByIdsResponse> GetProductsByIdsAsync(IEnumerable<Guid> productIds);

	/// <summary>
	/// Get product by ID for AI analysis
	/// </summary>
	Task<NatsProductResponse?> GetProductByIdAsync(Guid productId);
}
