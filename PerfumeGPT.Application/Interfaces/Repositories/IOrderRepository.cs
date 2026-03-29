using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOrderRepository : IGenericRepository<Order>
	{
		Task<(List<OrderListItem> Orders, int TotalCount)> GetPagedOrdersAsync(GetPagedOrdersRequest request, Guid? userId = null, Guid? staffId = null);
		Task<OrderResponse?> GetOrderWithFullDetailsAsync(Guid orderId);
		Task<UserOrderResponse?> GetUserOrderWithFullDetailsAsync(Guid orderId, Guid userId);
		Task<ReceiptResponse?> GetInvoiceAsync(Guid orderId);
		Task<ReceiptResponse?> GetUserInvoiceAsync(Guid orderId, Guid userId);
		Task<(string CustomerEmail, ReceiptResponse Invoice)?> GetOnlineOrderInvoiceEmailPayloadAsync(Guid orderId);
		Task<Order?> GetOrderForStatusUpdateAsync(Guid orderId);
		Task<Order?> GetOrderForCancellationAsync(Guid orderId);
		Task<Order?> GetOrderForMarkUsedVoucherAsync(Guid orderId);
		Task<Order?> GetPaidOrderForPickListAsync(Guid orderId);
		Task<Order?> GetOrderForFulfillmentAsync(Guid orderId);
		Task<Order?> GetOrderForSwapDamagedStockAsync(Guid orderId);
		Task<Order?> GetOrderWithDetailsForShippingAsync(Guid orderId);
	}
}
