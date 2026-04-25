using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Cart operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsCartService : INatsCartService
{
	private readonly INatsCartRepository _cartRepository;

	public NatsCartService(INatsCartRepository cartRepository)
	{
		_cartRepository = cartRepository;
	}

	public async Task<NatsCartResponse?> GetCartByUserIdAsync(Guid userId)
	{
		return await _cartRepository.GetCartByUserIdForNatsAsync(userId);
	}
}
