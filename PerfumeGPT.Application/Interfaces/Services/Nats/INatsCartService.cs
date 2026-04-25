using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Cart operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsCartService
{
	/// <summary>
	/// Get cart by user ID for AI analysis
	/// </summary>
	Task<NatsCartResponse?> GetCartByUserIdAsync(Guid userId);
}
