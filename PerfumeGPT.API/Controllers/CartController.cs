using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CartController : BaseApiController
	{
		private readonly ICartService _cartService;
		private readonly ICartItemService _cartItemService;

		public CartController(
			ICartService cartService,
			ICartItemService cartItemService)
		{
			_cartService = cartService;
			_cartItemService = cartItemService;
		}

		[HttpPost("pos-preview")]
		[ProducesResponseType(typeof(BaseResponse<PreviewPosOrderResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PreviewPosOrderResponse>>> GetCartPosPreview([FromBody] PreviewPosOrderRequest request)
		{
			var result = await _cartService.PreviewPosOrderAsync(request);
			return HandleResponse(result);
		}

		[HttpGet("items")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<GetCartItemsResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<GetCartItemsResponse>>> GetCartItems([FromQuery] GetPagedCartItemsRequest request)
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.GetCartItemsAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpGet("total")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<GetCartTotalResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<GetCartTotalResponse>>> GetCartTotal([FromQuery] GetCartTotalRequest request)
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.GetCartTotalAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpDelete("clear")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ClearCart([FromQuery] List<Guid>? itemIds)
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.ClearCartAsync(userId, itemIds);
			return HandleResponse(result);
		}

		[HttpPost("items")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> AddToCartAsync([FromBody] CreateCartItemRequest request)
		{
			var userId = GetCurrentUserId();
			var result = await _cartItemService.AddToCartAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpPut("items/{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCartItemAsync([FromRoute] Guid id, [FromBody] UpdateCartItemRequest request)
		{
			var userId = GetCurrentUserId();
			var result = await _cartItemService.UpdateCartItemAsync(userId, id, request);
			return HandleResponse(result);
		}

		[HttpDelete("items/{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> RemoveFromCartAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _cartItemService.RemoveFromCartAsync(userId, id);
			return HandleResponse(result);
		}
	}
}
