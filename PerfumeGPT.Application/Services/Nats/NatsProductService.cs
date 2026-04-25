using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Product operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsProductService : INatsProductService
{
	private readonly INatsProductRepository _productRepository;

	public NatsProductService(INatsProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public async Task<NatsProductByIdsResponse> GetProductsByIdsAsync(IEnumerable<Guid> productIds)
	{
		var items = await _productRepository.GetProductsByIdsForNatsAsync(productIds);
		return new NatsProductByIdsResponse { Items = items };
	}

	public async Task<NatsProductResponse?> GetProductByIdAsync(Guid productId)
	{
		return await _productRepository.GetProductByIdForNatsAsync(productId);
	}
}
