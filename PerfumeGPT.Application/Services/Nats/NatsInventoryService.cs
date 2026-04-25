using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Inventory operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsInventoryService : INatsInventoryService
{
	private readonly INatsInventoryRepository _inventoryRepository;

	public NatsInventoryService(INatsInventoryRepository inventoryRepository)
	{
		_inventoryRepository = inventoryRepository;
	}

	public async Task<NatsInventoryPagedResponse> GetPagedInventoryAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		int? brandId = null,
		int? categoryId = null,
		string? stockStatus = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var (items, totalCount) = await _inventoryRepository.GetPagedInventoryForNatsAsync(
			pageNumber,
			pageSize,
			variantId,
			brandId,
			categoryId,
			stockStatus,
			sortBy,
			isDescending);

		return new NatsInventoryPagedResponse
		{
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize,
			TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
			Items = items
		};
	}

	public async Task<NatsInventoryOverallStats> GetOverallStatsAsync()
	{
		return await _inventoryRepository.GetOverallStatsForNatsAsync();
	}
}
