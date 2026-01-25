using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/cart/items")]
	[ApiController]
	[Authorize]
	public class CartItemsController : BaseApiController
	{
		private readonly ICartItemService _cartItemService;

		public CartItemsController(ICartItemService cartItemService)
		{
			_cartItemService = cartItemService;
		}

		[HttpPost("add-to-cart")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> AddToCartAsync([FromBody] CreateCartItemRequest request)
		{
			var validation = ValidateRequestBody<CreateCartItemRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var result = await _cartItemService.AddToCartAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}/update-cart-item")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCartItemAsync(
			[FromRoute] Guid id,
			[FromBody] UpdateCartItemRequest request)
		{
			var validation = ValidateRequestBody<UpdateCartItemRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var result = await _cartItemService.UpdateCartItemAsync(userId, id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id:guid}/remove-from-cart")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> RemoveFromCartAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _cartItemService.RemoveFromCartAsync(userId, id);
			return HandleResponse(result);
		}
	}
}
