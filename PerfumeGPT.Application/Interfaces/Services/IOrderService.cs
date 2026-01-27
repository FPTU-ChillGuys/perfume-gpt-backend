using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderService
	{
		Task<BaseResponse<string>> Checkout(Guid userId, CreateOrderRequest request);
		Task<BaseResponse<string>> CheckoutInStore(Guid staffId, CreateInStoreOrderRequest request);
		Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request);
		Task<BaseResponse<string>> UpdateOrderStatusAsync(Guid orderId, Guid staffId, UpdateOrderStatusRequest request);
		Task<BaseResponse<string>> CancelOrderAsync(Guid orderId, Guid userId);
	}
}
