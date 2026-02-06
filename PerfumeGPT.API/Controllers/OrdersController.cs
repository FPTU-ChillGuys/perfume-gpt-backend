using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	/// <summary>
	/// Manages order operations including checkout, status updates, and fulfillment.
	/// </summary>
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

		/// <summary>
		/// Get all orders with pagination and filtering (Staff/Admin only).
		/// </summary>
		/// <param name="request">Paging and filtering parameters</param>
		/// <returns>Paginated list of orders</returns>
		[HttpGet]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var response = await _orderService.GetOrdersAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get a single order by ID with full details (Staff/Admin only).
		/// </summary>
		/// <param name="orderId">The order ID</param>
		/// <returns>Order details</returns>
		[HttpGet("{orderId}")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<OrderResponse>>> GetOrderById([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetOrderByIdAsync(orderId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get orders for the current authenticated user.
		/// </summary>
		/// <param name="request">Paging and filtering parameters</param>
		/// <returns>Paginated list of user's orders</returns>
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

		/// <summary>
		/// Get orders by customer ID (Staff/Admin only).
		/// </summary>
		/// <param name="userId">The customer's user ID</param>
		/// <param name="request">Paging and filtering parameters</param>
		/// <returns>Paginated list of customer's orders</returns>
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

		/// <summary>
		/// Get orders handled by a specific staff member (Admin only).
		/// </summary>
		/// <param name="staffId">The staff's user ID</param>
		/// <param name="request">Paging and filtering parameters</param>
		/// <returns>Paginated list of staff's orders</returns>
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

		/// <summary>
		/// Checkout order for authenticated user (online order).
		/// </summary>
		/// <param name="request">Order details including payment and shipping info</param>
		/// <returns>Payment URL or order confirmation</returns>
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

		/// <summary>
		/// Checkout order in store (Staff only - offline/POS order).
		/// </summary>
		/// <param name="request">In-store order details</param>
		/// <returns>Payment URL or order confirmation</returns>
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

		/// <summary>
		/// Preview order with calculated totals (for POS system).
		/// </summary>
		/// <param name="request">Preview request with barcodes and optional voucher</param>
		/// <returns>Calculated order preview with subtotal, shipping, discount, and total</returns>
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

		#endregion

		#region Order Status Management

		/// <summary>
		/// Update order status (Staff only).
		/// When transitioning to Processing status, returns a pick list with batch codes and locations.
		/// </summary>
		/// <param name="orderId">The order ID</param>
		/// <param name="request">New status and optional note</param>
		/// <returns>Pick list response when status is Processing, otherwise status update confirmation</returns>
		[HttpPut("{orderId}/status")]
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

		/// <summary>
		/// Cancel an order (User only - for their own orders).
		/// Only Pending or Processing orders can be cancelled.
		/// </summary>
		/// <param name="orderId">The order ID to cancel</param>
		/// <returns>Cancellation confirmation</returns>
		[HttpPost("{orderId}/cancel")]
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

		#endregion

		#region Order Fulfillment (Warehouse Operations)

		/// <summary>
		/// Fulfill an order after staff has picked and verified all items (Staff only).
		/// This commits the stock reservation, updates status to Shipped, and creates GHN shipping order.
		/// </summary>
		/// <param name="orderId">The order ID to fulfill</param>
		/// <param name="request">Scanned batch codes for verification</param>
		/// <returns>Fulfillment confirmation</returns>
		[HttpPost("{orderId}/fulfill")]
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

		/// <summary>
		/// Swap a damaged stock item with the next available batch (Staff only).
		/// Uses FEFO logic to find replacement batch, creates stock adjustment for damaged item.
		/// </summary>
		/// <param name="orderId">The order ID</param>
		/// <param name="request">Details of the damaged reservation and optional note</param>
		/// <returns>New batch information for picking</returns>
		[HttpPost("{orderId}/swap-damaged")]
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
