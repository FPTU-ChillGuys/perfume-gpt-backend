using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services
{
	/// <summary>
	/// Core order service handling checkout, status updates, and cancellations.
	/// Fulfillment operations are delegated to IOrderFulfillmentService.
	/// </summary>
	public interface IOrderService
	{
		#region Query Operations

		/// <summary>
		/// Gets a paginated list of all orders with optional filtering.
		/// </summary>
		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request);

		/// <summary>
		/// Gets a single order by its ID with full details.
		/// </summary>
		Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId);

		/// <summary>
		/// Gets all orders for a specific customer with optional filtering.
		/// </summary>
		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByUserIdAsync(Guid userId, GetPagedOrdersRequest request);

		/// <summary>
		/// Gets all orders handled by a specific staff member.
		/// </summary>
		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByStaffIdAsync(Guid staffId, GetPagedOrdersRequest request);

		#endregion

		#region Checkout Operations

		Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request);

		Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request);

		Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request);

		#endregion

		#region Order Status Management

		Task<BaseResponse<PickListResponse>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request);

		Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId);

		#endregion

		#region Fulfillment Operations (delegated to IOrderFulfillmentService)

		/// <summary>
		/// Fulfills an order after staff has picked all items and verified batch codes.
		/// Commits stock reservation and creates GHN shipping order.
		/// </summary>
		Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);

		/// <summary>
		/// Swaps a damaged stock item with the next available batch using FEFO logic.
		/// Creates a stock adjustment for the damaged batch and re-reserves from a new batch.
		/// </summary>
		Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);

		#endregion
	}
}
