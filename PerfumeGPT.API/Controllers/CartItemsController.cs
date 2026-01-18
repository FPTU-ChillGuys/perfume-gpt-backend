using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartItemsController : BaseApiController
    {
        private readonly ICartItemService _cartItemService;

        public CartItemsController(ICartItemService cartItemService)
        {
            _cartItemService = cartItemService;
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponse<string>>> AddToCartAsync([FromBody] CreateCartItemRequest request)
        {
            var validation = ValidateRequestBody<CreateCartItemRequest>(request);
            if (validation != null) return validation;

            var result = await _cartItemService.AddToCartAsync(request);
            return HandleResponse(result);
        }

        [HttpPut("{cartItemId}")]
        public async Task<ActionResult<BaseResponse<string>>> UpdateCartAsync([FromRoute] Guid cartItemId, [FromBody] UpdateCartItemRequest request)
        {
            var validation = ValidateRequestBody<UpdateCartItemRequest>(request);
            if (validation != null) return validation;

            var userId = GetCurrentUserId();
            var result = await _cartItemService.UpdateCart(userId, cartItemId, request);
            return HandleResponse(result);
        }

        [HttpDelete("{cartItemId}")]
        public async Task<ActionResult<BaseResponse<string>>> RemoveFromCartAsync([FromRoute] Guid cartItemId)
        {
            var userId = GetCurrentUserId();

            var result = await _cartItemService.RemoveFromCartAsync(userId, cartItemId);
            return HandleResponse(result);
        }

    }
}
