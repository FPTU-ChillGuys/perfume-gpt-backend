using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Repositories.Nats;

/// <summary>
/// NATS-specific repository interface for Order operations
/// Returns NATS-optimized DTOs that match AI backend expectations
/// </summary>
public interface INatsOrderRepository
{
	/// <summary>
	/// Get paged orders for AI analysis
	/// </summary>
	Task<(List<NatsOrderListItemResponse> Items, int TotalCount)> GetPagedOrdersForNatsAsync(
		int pageNumber,
		int pageSize,
		Guid? userId = null,
		string? status = null,
		string? paymentStatus = null,
		int? shippingStatus = null,
		string? sortBy = null,
		bool isDescending = false);

	/// <summary>
	/// Get order by ID for AI analysis
	/// </summary>
	Task<NatsOrderListItemResponse?> GetOrderByIdForNatsAsync(Guid orderId);
}
