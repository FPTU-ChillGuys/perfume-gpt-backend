using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Inventory operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsInventoryRepository
{
	/// <summary>
	/// Get paged inventory for AI analysis
	/// </summary>
	Task<(List<NatsInventoryStockResponse> Items, int TotalCount)> GetPagedInventoryForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? variantId = null,
		int? brandId = null,
		int? categoryId = null,
		string? stockStatus = null,
		string? sortBy = null,
		bool isDescending = false);

	/// <summary>
	/// Get inventory overall stats for AI analysis
	/// </summary>
	Task<NatsInventoryOverallStats> GetOverallStatsForNatsAsync();
}
