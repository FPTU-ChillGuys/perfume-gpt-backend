using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderFulfillmentService
	{
		Task<PickListResponse> GetPickListAsync(Order order);
		Task<string> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);
		Task<SwapDamagedStockResponse> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);
	}
}
