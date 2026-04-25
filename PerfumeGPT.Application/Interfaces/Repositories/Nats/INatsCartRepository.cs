using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Cart operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsCartRepository
{
	/// <summary>
	/// Get cart by user ID for AI analysis
	/// </summary>
	Task<NatsCartResponse?> GetCartByUserIdForNatsAsync(Guid userId);
}
