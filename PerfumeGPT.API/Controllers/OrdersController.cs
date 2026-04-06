using FluentValidation;
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
		private readonly IValidator<UpdateOrderAddressRequest> _updateOrderAddressValidator;
		private readonly IValidator<GetPagedOrdersRequest> _pagedOrdersValidator;
		private readonly IValidator<CreateOrderRequest> _checkoutValidator;
		private readonly IValidator<CreateInStoreOrderRequest> _checkoutInStoreValidator;
		private readonly IValidator<PreviewOrderRequest> _previewOrderValidator;
		private readonly IValidator<UpdateOrderStatusRequest> _updateOrderStatusValidator;
		private readonly IValidator<UserCancelOrderRequest> _cancelOrderValidator;
		private readonly IValidator<FulfillOrderRequest> _fulfillOrderValidator;
		private readonly IValidator<SwapDamagedStockRequest> _swapDamagedStockValidator;

		public OrdersController(
			IOrderService orderService,
			IValidator<UpdateOrderAddressRequest> updateOrderAddressValidator,
			IValidator<GetPagedOrdersRequest> pagedOrdersValidator,
			IValidator<CreateOrderRequest> checkoutValidator,
			IValidator<CreateInStoreOrderRequest> checkoutInStoreValidator,
			IValidator<PreviewOrderRequest> previewOrderValidator,
			IValidator<UpdateOrderStatusRequest> updateOrderStatusValidator,
			IValidator<UserCancelOrderRequest> cancelOrderValidator,
			IValidator<FulfillOrderRequest> fulfillOrderValidator,
			IValidator<SwapDamagedStockRequest> swapDamagedStockValidator)
		{
			_orderService = orderService;
			_updateOrderAddressValidator = updateOrderAddressValidator;
			_pagedOrdersValidator = pagedOrdersValidator;
			_checkoutValidator = checkoutValidator;
			_checkoutInStoreValidator = checkoutInStoreValidator;
			_previewOrderValidator = previewOrderValidator;
			_updateOrderStatusValidator = updateOrderStatusValidator;
			_cancelOrderValidator = cancelOrderValidator;
			_fulfillOrderValidator = fulfillOrderValidator;
			_swapDamagedStockValidator = swapDamagedStockValidator;
		}

		#region User Query Operations
		[HttpGet("my-orders")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetMyOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var validation = await ValidateRequestAsync(_pagedOrdersValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var requestWithUserId = request with { UserId = userId };

			var response = await _orderService.GetOrdersAsync(requestWithUserId);
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

		[HttpGet("my-orders/{orderId:guid}/invoice")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ReceiptResponse>>> GetMyOrderInvoice([FromRoute] Guid orderId)
		{
			var userId = GetCurrentUserId();
			var response = await _orderService.GetMyInvoiceAsync(orderId, userId);
			return HandleResponse(response);
		}
		#endregion User Query Operations



		#region Staff/Admin Query Operations
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

		[HttpGet("{orderId:guid}/invoice")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ReceiptResponse>>> GetOrderInvoice([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetInvoiceAsync(orderId);
			return HandleResponse(response);
		}
		#endregion Staff/Admin Query Operations



		#region Checkout Operations
		[HttpPost("checkout")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> Checkout([FromBody] CreateOrderRequest request)
		{
			var validation = await ValidateRequestAsync(_checkoutValidator, request);
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
			var validation = await ValidateRequestAsync(_checkoutInStoreValidator, request);
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
			var validation = await ValidateRequestAsync(_previewOrderValidator, request);
			if (validation != null) return validation;

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
		public async Task<ActionResult<BaseResponse<PickListResponse>>> UpdateOrderStatus([FromRoute] Guid orderId, [FromBody] UpdateOrderStatusRequest request)
		{
			var validation = await ValidateRequestAsync(_updateOrderStatusValidator, request);
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
		public async Task<ActionResult<BaseResponse<string>>> CancelOrder([FromRoute] Guid orderId, [FromBody] UserCancelOrderRequest request)
		{
			var validation = await ValidateRequestAsync(_cancelOrderValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _orderService.CancelOrderAsync(orderId, userId, request);
			return HandleResponse(response);
		}

		[HttpPut("{orderId:guid}/address")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateOrderAddress([FromRoute] Guid orderId, [FromBody] UpdateOrderAddressRequest request)
		{
			var validation = await ValidateRequestAsync(_updateOrderAddressValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _orderService.UpdateOrderAddressAsync(orderId, userId, request);
			return HandleResponse(response);
		}
		#endregion Order Status Management



		#region Order Fulfillment (Warehouse Operations)
		[HttpGet("{orderId:guid}/picklist")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PickListResponse>>> GetOrderPickList([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetOrderPickListAsync(orderId);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/fulfill")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> FulfillOrder([FromRoute] Guid orderId, [FromBody] FulfillOrderRequest request)
		{
			var validation = await ValidateRequestAsync(_fulfillOrderValidator, request);
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
		public async Task<ActionResult<BaseResponse<SwapDamagedStockResponse>>> SwapDamagedStock([FromRoute] Guid orderId, [FromBody] SwapDamagedStockRequest request)
		{
			var validation = await ValidateRequestAsync(_swapDamagedStockValidator, request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _orderService.SwapDamagedStockAsync(orderId, staffId, request);
			return HandleResponse(response);
		}
		#endregion Order Fulfillment (Warehouse Operations)
	}
}
