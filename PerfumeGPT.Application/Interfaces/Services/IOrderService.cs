using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOrderService
	{
		Task<BaseResponse<string>> Checkout(CreateOrderRequest request);
		Task<BaseResponse<string>> CheckoutInStore(CreateInStoreOrderRequest request);
		Task<BaseResponse<PreviewOrderResponse>> PreviewOrder(PreviewOrderRequest request);
	}
}
