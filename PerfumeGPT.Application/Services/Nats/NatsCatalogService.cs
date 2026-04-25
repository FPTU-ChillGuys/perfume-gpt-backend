using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Sourcing Catalog operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsCatalogService : INatsCatalogService
{
	private readonly INatsCatalogRepository _catalogRepository;

	public NatsCatalogService(INatsCatalogRepository catalogRepository)
	{
		_catalogRepository = catalogRepository;
	}

	public async Task<NatsCatalogResponse> GetCatalogsByVariantIdAsync(Guid variantId)
	{
		var catalogs = await _catalogRepository.GetCatalogsByVariantIdForNatsAsync(variantId);
		return new NatsCatalogResponse { Catalogs = catalogs };
	}
}
