using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Repositories.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Application.Services.Nats;

/// <summary>
/// NATS-specific service implementation for Order operations
/// Provides AI-optimized responses through NATS messaging
/// </summary>
public sealed class NatsOrderService : INatsOrderService
{
	private readonly INatsOrderRepository _orderRepository;

	public NatsOrderService(INatsOrderRepository orderRepository)
	{
		_orderRepository = orderRepository;
	}

	public async Task<NatsOrderPagedResponse> GetPagedOrdersAsync(
		int pageNumber,
		int pageSize,
		Guid? userId = null,
		string? status = null,
		string? paymentStatus = null,
		string? shippingStatus = null,
		string? sortBy = null,
		bool isDescending = false)
	{
		var (items, totalCount) = await _orderRepository.GetPagedOrdersForNatsAsync(
			pageNumber,
			pageSize,
			userId,
			status,
			paymentStatus,
			shippingStatus,
			sortBy,
			isDescending);

		return new NatsOrderPagedResponse
		{
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize,
			TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
			Items = items
		};
	}

	public async Task<NatsOrderListItemResponse?> GetOrderByIdAsync(Guid orderId)
	{
		return await _orderRepository.GetOrderByIdForNatsAsync(orderId);
	}
}
