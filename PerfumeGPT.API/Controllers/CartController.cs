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

		[HttpGet]
		public async Task<ActionResult<BaseResponse<GetCartResponse>>> GetCart([FromQuery] Guid? voucherId)
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.GetCartByUserIdAsync(userId, voucherId);
			return HandleResponse(result);
		}

		[HttpDelete]
		public async Task<ActionResult<BaseResponse<string>>> ClearCart()
		{
			var userId = GetCurrentUserId();
			var result = await _cartService.ClearCartAsync(userId);
			return HandleResponse(result);
		}
	}
}
