using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ShippingsController : BaseApiController
	{
		private readonly IShippingService _shippingService;
		private readonly IGHNService _ghnService;
		private readonly IValidator<GetOrderInfoRequest> _getOrderInfoRequestValidator;

		public ShippingsController(
			IShippingService shippingService,
			IGHNService ghnService,
			IValidator<GetOrderInfoRequest> getOrderInfoRequestValidator)
		{
			_shippingService = shippingService;
			_ghnService = ghnService;
			_getOrderInfoRequestValidator = getOrderInfoRequestValidator;
		}

		[HttpGet("user/{userId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ShippingInfoListItem>>>> GetPagedShippingsByUserId([FromRoute] Guid userId, [FromQuery] GetPagedShippingsRequest request)
		{
			var response = await _shippingService.GetPagedShippingInfosByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("me")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ShippingInfoListItem>>>> GetPagedShippingsForCurrentUser([FromQuery] GetPagedShippingsRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _shippingService.GetPagedShippingInfosByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpPost("user/{userId:guid}/sync-shipping-status")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SyncShippingStatusByUserId([FromRoute] Guid userId)
		{
			var response = await _shippingService.SyncShippingStatusByUserIdAsync(userId);
			return HandleResponse(response);
		}

		[HttpPost("me/sync-shipping-status")]
		public async Task<ActionResult<BaseResponse<string>>> SyncShippingStatusForCurrentUser()
		{
			var userId = GetCurrentUserId();

			var response = await _shippingService.SyncShippingStatusByUserIdAsync(userId);
			return HandleResponse(response);
		}

		[HttpPost("order-info-url")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> GetOrderInfoUrlAsync([FromBody] GetOrderInfoRequest request)
		{
			var validation = await ValidateRequestAsync(_getOrderInfoRequestValidator, request);
			if (validation != null) return validation;

			var response = await _ghnService.GetOrderInfoUrlAsync(request);
			return HandleResponse(response);
		}
	}
}
