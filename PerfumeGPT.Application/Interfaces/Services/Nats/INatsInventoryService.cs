using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Inventory operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsInventoryService
{
	/// <summary>
	/// Get paged inventory for AI analysis
	/// </summary>
	Task<NatsInventoryPagedResponse> GetPagedInventoryAsync(
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
	Task<NatsInventoryOverallStats> GetOverallStatsAsync();
}
