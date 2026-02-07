using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOrderRepository : IGenericRepository<Order>
	{
		Task<(List<OrderListItem> Orders, int TotalCount)> GetPagedOrdersAsync(
			GetPagedOrdersRequest request,
			Guid? userId = null,
			Guid? staffId = null);
		Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId);
		Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId);
		Task<Order?> GetOrderForCancellationAsync(Guid orderId);
	}
}
