using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Hubs;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrdersController : BaseApiController
	{
		private readonly IOrderService _orderService;
		private readonly IHubContext<PosHub, IPosClient> _posHubContext;

		public OrdersController(
			IOrderService orderService,
			IHubContext<PosHub, IPosClient> posHubContext)
		{
			_orderService = orderService;
			_posHubContext = posHubContext;
		}

		#region User Query Operations
		[HttpGet("my-orders")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetMyOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var userId = GetCurrentUserId();
			var requestWithUserId = request with { UserId = userId };

			var response = await _orderService.GetOrdersAsync(requestWithUserId);
			return HandleResponse(response);
		}

		[HttpGet("my-orders/{orderId:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<UserOrderResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<UserOrderResponse>>> GetMyOrderById([FromRoute] Guid orderId)
		{
			var userId = GetCurrentUserId();
			var response = await _orderService.GetUserOrderByIdAsync(orderId, userId);
			return HandleResponse(response);
		}

		[HttpGet("my-orders/{orderId:guid}/invoice")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
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
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderListItem>>>> GetOrders([FromQuery] GetPagedOrdersRequest request)
		{
			var response = await _orderService.GetOrdersAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("order-code/{code}")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<OrderResponse>>> GetOrderForPosPickup([FromRoute] string code)
		{
			var validationResult = ValidateRequiredString(code, "Mã đơn hàng");
			if (validationResult != null) return validationResult;

			var response = await _orderService.GetOrderForPosPickupAsync(code);
			return HandleResponse(response);
		}

		[HttpGet("{orderId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<OrderResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<OrderResponse>>> GetOrderById([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetOrderByIdAsync(orderId);
			return HandleResponse(response);
		}

		[HttpGet("{orderId:guid}/invoice")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<ReceiptResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ReceiptResponse>>> GetOrderInvoice([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetInvoiceAsync(orderId);
			return HandleResponse(response);
		}
		#endregion Staff/Admin Query Operations



		#region Checkout Operations
		[HttpPost("checkout")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<CreatePaymentResponseDto>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CreatePaymentResponseDto>>> Checkout([FromBody] CreateOrderRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _orderService.Checkout(userId, request);
			return HandleResponse(response);
		}

		[HttpPost("checkout-in-store")]
		[Authorize(Roles = "staff")]
		[ProducesResponseType(typeof(BaseResponse<CreatePaymentResponseDto>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CreatePaymentResponseDto>>> CheckoutInStore([FromBody] CreateInStoreOrderRequest request)
		{
			var staffId = GetCurrentUserId();

			var response = await _orderService.CheckoutInStore(staffId, request);
			return HandleResponse(response);
		}
		#endregion Checkout Operations



		#region Order Status Management
		[HttpPut("{orderId:guid}/staff-prepare")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PickListResponse>>> UpdateOrderStatus([FromRoute] Guid orderId)
		{
			var staffId = GetCurrentUserId();

			var response = await _orderService.UpdateOrderStatusToPreparingAsync(orderId, staffId);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/staff-cancel")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CancelOrderByStaff([FromRoute] Guid orderId, [FromBody] StaffCancelOrderRequest request)
		{
			var staffId = GetCurrentUserId();

			var response = await _orderService.CancelOrderByStaffAsync(orderId, staffId, request);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/cancel")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CancelOrder([FromRoute] Guid orderId, [FromBody] UserCancelOrderRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _orderService.CancelOrderAsync(orderId, userId, request);
			return HandleResponse(response);
		}
		#endregion Order Status Management



		#region Order Fulfillment (Warehouse Operations)
		[HttpGet("{orderId:guid}/picklist")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PickListResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PickListResponse>>> GetOrderPickList([FromRoute] Guid orderId)
		{
			var response = await _orderService.GetOrderPickListAsync(orderId);
			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/fulfill")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> FulfillOrder([FromRoute] Guid orderId, [FromBody] FulfillOrderRequest request)
		{
			var staffId = GetCurrentUserId();

			var response = await _orderService.FulfillOrderAsync(orderId, staffId, request);
			return HandleResponse(response);
		}

		[HttpPut("{orderId:guid}/deliver-in-store")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeliverOrderToInStoreCustomer([FromRoute] Guid orderId, [FromBody] DeliverInStoreRequest request)
		{
			var staffId = GetCurrentUserId();
			var response = await _orderService.DeliverOrderToInStoreCustomerAsync(orderId, staffId);

			if (response.Success && !string.IsNullOrWhiteSpace(request.PosSessionId) && !string.IsNullOrWhiteSpace(response.Payload))
				await _posHubContext.Clients.Group(request.PosSessionId).OrderDelivered(response.Payload);

			return HandleResponse(response);
		}

		[HttpPost("{orderId:guid}/swap-damaged")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<SwapDamagedStockResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<SwapDamagedStockResponse>>> SwapDamagedStock([FromRoute] Guid orderId, [FromBody] SwapDamagedStockRequest request)
		{
			var staffId = GetCurrentUserId();

			var response = await _orderService.SwapDamagedStockAsync(orderId, staffId, request);
			return HandleResponse(response);
		}
		#endregion Order Fulfillment (Warehouse Operations)
	}
}
