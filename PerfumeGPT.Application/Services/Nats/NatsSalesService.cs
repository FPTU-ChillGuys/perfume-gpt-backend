using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Sales operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsSalesService : INatsSalesService
{
	private readonly INatsSalesRepository _salesRepository;

	public NatsSalesService(INatsSalesRepository salesRepository)
	{
		_salesRepository = salesRepository;
	}

	public async Task<NatsSalesAnalyticsResponse?> GetSalesAnalyticsByVariantIdAsync(Guid variantId)
	{
		return await _salesRepository.GetSalesAnalyticsByVariantIdForNatsAsync(variantId);
	}
}
