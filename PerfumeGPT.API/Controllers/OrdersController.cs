using Microsoft.AspNetCore.Authorization;
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
		[Authorize(Roles = "user")]
		public async Task<ActionResult<BaseResponse<string>>> Checkout([FromBody] CreateOrderRequest request)
		{
			var validation = ValidateRequestBody<CreateOrderRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _orderService.Checkout(userId, request);
			return HandleResponse(response);
		}

		[HttpPost("checkout-in-store")]
		[Authorize(Roles = "staff")]
		public async Task<ActionResult<BaseResponse<string>>> CheckoutInStore([FromBody] CreateInStoreOrderRequest request)
		{
			var validation = ValidateRequestBody<CreateInStoreOrderRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.CheckoutInStore(staffId, request);
			return HandleResponse(response);
		}

	[HttpGet("preview")]
	public async Task<ActionResult<BaseResponse<PreviewOrderResponse>>> PreviewOrder([FromQuery] PreviewOrderRequest request)
	{
		var response = await _orderService.PreviewOrder(request);
		return HandleResponse(response);
	}

	[HttpPut("{orderId}/status")]
	[Authorize(Roles = "staff")]
	public async Task<ActionResult<BaseResponse<string>>> UpdateOrderStatus(
		[FromRoute] Guid orderId,
		[FromBody] UpdateOrderStatusRequest request)
	{
		var validation = ValidateRequestBody<UpdateOrderStatusRequest>(request);
		if (validation != null) return validation;

		var staffId = GetCurrentUserId();
		var response = await _orderService.UpdateOrderStatusAsync(orderId, staffId, request);
		return HandleResponse(response);
	}

	[HttpPost("{orderId}/cancel")]
	[Authorize(Roles = "user")]
	public async Task<ActionResult<BaseResponse<string>>> CancelOrder([FromRoute] Guid orderId)
	{
		var userId = GetCurrentUserId();
		var response = await _orderService.CancelOrderAsync(orderId, userId);
		return HandleResponse(response);
	}
	}
}
