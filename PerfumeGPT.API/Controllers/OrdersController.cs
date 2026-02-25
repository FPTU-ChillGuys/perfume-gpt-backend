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

		#region Query Operations

		[HttpGet]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var response = await _orderService.GetOrdersAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{orderId:guid}")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<OrderResponse>>> GetOrderById([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetOrderByIdAsync(orderId);
			return HandleResponse(response);
		}

		[HttpGet("my-orders")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetMyOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _orderService.GetOrdersByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("my-orders/{orderId:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<UserOrderResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<UserOrderResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<UserOrderResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<UserOrderResponse>>> GetMyOrderById([FromRoute] Guid orderId)
		{
			var userId = GetCurrentUserId();
			var response = await _orderService.GetUserOrderByIdAsync(orderId, userId);
			return HandleResponse(response);
		}

		[HttpGet("user/{userId}")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetOrdersByUserId(
			[FromRoute] Guid userId,
			[FromQuery] GetPagedOrdersRequest request)
		{
			var response = await _orderService.GetOrdersByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("staff/{staffId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetOrdersByStaffId(
			[FromRoute] Guid staffId,
			[FromQuery] GetPagedOrdersRequest request)
		{
			var response = await _orderService.GetOrdersByStaffIdAsync(staffId, request);
			return HandleResponse(response);
		}

		#endregion

		#region Checkout Operations

		[HttpPost("checkout")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
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
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CheckoutInStore([FromBody] CreateInStoreOrderRequest request)
		{
			var validation = ValidateRequestBody<CreateInStoreOrderRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.CheckoutInStore(staffId, request);
			return HandleResponse(response);
		}

		[HttpGet("preview")]
		[ProducesResponseType(typeof(BaseResponse<PreviewOrderResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PreviewOrderResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<PreviewOrderResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<PreviewOrderResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PreviewOrderResponse>>> PreviewOrder([FromQuery] PreviewOrderRequest request)
		{
			var response = await _orderService.PreviewOrder(request);
			return HandleResponse(response);
		}

		#endregion Checkout Operations

		#region Order Status Management

		[HttpPut("{orderId:guid}/status")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PickListResponse>>> UpdateOrderStatus(
			[FromRoute] Guid orderId,
			[FromBody] UpdateOrderStatusRequest request)
		{
			var validation = ValidateRequestBody<UpdateOrderStatusRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.UpdateOrderStatusAsync(orderId, staffId, request);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/cancel")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CancelOrder([FromRoute] Guid orderId)
		{
			var userId = GetCurrentUserId();
			var response = await _orderService.CancelOrderAsync(orderId, userId);
			return HandleResponse(response);
		}

		[HttpPut("{orderId:guid}/address")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateOrderAddress([FromRoute] Guid orderId, [FromBody] RecipientInformation request)
		{
			var validation = ValidateRequestBody<RecipientInformation>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _orderService.UpdateOrderAddressAsync(orderId, userId, request);
			return HandleResponse(response);
		}

		#endregion

		#region Order Fulfillment (Warehouse Operations)

		[HttpPost("{orderId:guid}/fulfill")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> FulfillOrder(
			[FromRoute] Guid orderId,
			[FromBody] FulfillOrderRequest request)
		{
			var validation = ValidateRequestBody<FulfillOrderRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.FulfillOrderAsync(orderId, staffId, request);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/swap-damaged")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<SwapDamagedStockResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<SwapDamagedStockResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<SwapDamagedStockResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<SwapDamagedStockResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<SwapDamagedStockResponse>>> SwapDamagedStock(
			[FromRoute] Guid orderId,
			[FromBody] SwapDamagedStockRequest request)
		{
			var validation = ValidateRequestBody<SwapDamagedStockRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.SwapDamagedStockAsync(orderId, staffId, request);
			return HandleResponse(response);
		}

		#endregion
	}
}
