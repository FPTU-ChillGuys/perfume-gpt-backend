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
		Task<BaseResponse<OrderResponse>> GetOrderByCodeAsync(string orderCode);
		Task<BaseResponse<ReceiptResponse>> GetInvoiceAsync(Guid orderId);
		Task<BaseResponse<ReceiptResponse>> GetMyInvoiceAsync(Guid orderId, Guid userId);
		#endregion Query Operations

		#region Address Management
		Task<BaseResponse<string>> UpdateOrderAddressAsync(Guid orderId, Guid userId, UpdateOrderAddressRequest request);
		#endregion Address Management

		#region Checkout Operations
		Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request);
		Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request);
		#endregion Checkout Operations

		#region Order Status Management
		Task<BaseResponse<PickListResponse?>> UpdateOrderStatusToPreparingAsync(Guid orderId, Guid staffId);
		Task<BaseResponse<string>> CancelOrderByStaffAsync(Guid orderId, Guid staffId, StaffCancelOrderRequest request);
		Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId, UserCancelOrderRequest request);
		#endregion Order Status Management

		#region Fulfillment Operations (delegated to IOrderFulfillmentService)
		Task<BaseResponse<PickListResponse>> GetOrderPickListAsync(Guid orderId);
		Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request);
		Task<BaseResponse<string>> DeliverOrderToInStoreCustomerAsync(Guid orderId, Guid staffId);
		Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request);
		#endregion Fulfillment Operations (delegated to IOrderFulfillmentService)
	}
}
