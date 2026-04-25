using PerfumeGPT.Application.DTOs.Responses.Nats;

namespace PerfumeGPT.Application.Interfaces.Services.Nats;

/// <summary>
/// NATS-specific service interface for Order operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public interface INatsOrderService
{
	/// <summary>
	/// Get paged orders for AI analysis
	/// </summary>
	Task<NatsOrderPagedResponse> GetPagedOrdersAsync(
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
	Task<NatsOrderListItemResponse?> GetOrderByIdAsync(Guid orderId);
}
