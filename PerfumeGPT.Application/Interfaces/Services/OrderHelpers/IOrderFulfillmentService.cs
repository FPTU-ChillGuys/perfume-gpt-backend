using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderFulfillmentService
	{
		Task<PickListResponse> GetPickListAsync(Guid orderId);
		Task<string> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);
		Task<SwapDamagedStockResponse> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);
	}
}
