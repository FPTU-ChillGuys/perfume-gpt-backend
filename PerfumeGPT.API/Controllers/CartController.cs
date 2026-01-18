using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
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
        public async Task<ActionResult<BaseResponse<PagedResult<GetCartItemResponse>>>> GetPagedItemsAsync([FromQuery] GetPagedCartItemsRequest request)
        {
            var validation = ValidateRequestBody<GetPagedCartItemsRequest>(request);
            if (validation != null) return validation;

            var userId = GetCurrentUserId();
            var result = await _cartService.GetPagedCartItemsAsync(userId, request);
            return HandleResponse(result);
        }
    }
}
