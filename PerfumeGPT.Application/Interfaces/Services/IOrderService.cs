using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderService
	{
		#region Query Operations

		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersAsync(GetPagedOrdersRequest request);
		Task<BaseResponse<OrderResponse>> GetOrderByIdAsync(Guid orderId);
		Task<BaseResponse<UserOrderResponse>> GetUserOrderByIdAsync(Guid orderId, Guid userId);
		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByUserIdAsync(Guid userId, GetPagedOrdersRequest request);
		Task<BaseResponse<PagedResult<OrderListItem>>> GetOrdersByStaffIdAsync(Guid staffId, GetPagedOrdersRequest request);

		#endregion

		#region Address Management

		Task<BaseResponse<string>> UpdateOrderAddressAsync(Guid orderId, Guid userId, UpdateOrderAddressRequest request);

		#endregion

		#region Checkout Operations

		Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request);
		Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request);
		Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request);

		#endregion

		#region Order Status Management

		Task<BaseResponse<PickListResponse>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request);
		Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId, UserCancelOrderRequest request);

		#endregion

		#region Fulfillment Operations (delegated to IOrderFulfillmentService)

		Task<BaseResponse<PickListResponse>> GetOrderPickListAsync(Guid orderId);
		Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);
		Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);

		#endregion
	}
}
