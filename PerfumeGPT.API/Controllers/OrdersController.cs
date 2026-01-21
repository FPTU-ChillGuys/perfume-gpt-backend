using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrdersController : BaseApiController
	{
		private readonly IOrderService _orderService;

		public OrdersController(IOrderService orderService)
		{
			_orderService = orderService;
		}

		[HttpPost("checkout")]
		public async Task<ActionResult<BaseResponse<string>>> Checkout([FromBody] CreateOrderRequest request)
		{
			var response = await _orderService.Checkout(request);
			return HandleResponse(response);
		}

		[HttpPost("checkout-in-store")]
		public async Task<ActionResult<BaseResponse<string>>> CheckoutInStore([FromBody] CreateInStoreOrderRequest request)
		{
			var response = await _orderService.CheckoutInStore(request);
			return HandleResponse(response);
		}

		[HttpGet("preview")]
		public async Task<ActionResult<BaseResponse<PreviewOrderResponse>>> PreviewOrder([FromQuery] PreviewOrderRequest request)
		{
			var response = await _orderService.PreviewOrder(request);
			return HandleResponse(response);
		}
	}
}
