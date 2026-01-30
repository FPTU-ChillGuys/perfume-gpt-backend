using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
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

		public CartController(ICartService cartService)
		{
			_cartService = cartService;
		}

		[HttpGet("items")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<GetCartItemsResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<GetCartItemsResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<GetCartItemsResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<GetCartItemsResponse>>> GetCartItems()
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.GetCartItemsAsync(userId);
			return HandleResponse(result);
		}

		[HttpGet("total")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<GetCartTotalResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<GetCartTotalResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<GetCartTotalResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<GetCartTotalResponse>>> GetCartTotal([FromQuery] Guid? voucherId)
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.GetCartTotalAsync(userId, voucherId);
			return HandleResponse(result);
		}

		[HttpDelete("clear")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> ClearCart()
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.ClearCartAsync(userId);
			return HandleResponse(result);
		}
	}
}
