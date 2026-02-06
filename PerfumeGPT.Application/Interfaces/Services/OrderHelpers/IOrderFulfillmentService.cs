using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services.OrderHelpers
{
	public interface IOrderFulfillmentService
	{
		/// <summary>
		/// Generates a pick list for an order based on its stock reservations.
		/// </summary>
		/// <returns>Pick list with batch codes and locations</returns>
		Task<BaseResponse<PickListResponse>> GetPickListAsync(Guid orderId);

		/// <summary>
		/// Fulfills an order after staff has picked and verified all items.
		/// Commits stock reservation and triggers shipping order creation.
		/// </summary>
		Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);

		/// <summary>
		/// Swaps a damaged stock item with the next available batch using FEFO logic.
		/// Creates a stock adjustment and re-reserves from a new batch.
		/// </summary>
		Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);
	}
}
