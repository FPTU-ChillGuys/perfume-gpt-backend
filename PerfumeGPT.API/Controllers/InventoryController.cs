using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class InventoryController : BaseApiController
	{
		private readonly IStockService _stockService;

		public InventoryController(IStockService stockService)
		{
			_stockService = stockService;
		}

		[HttpGet("stock")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<StockResponse>>>> GetInventory([FromQuery] GetPagedInventoryRequest request)
		{
			var response = await _stockService.GetInventoryAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("stock/variant/{variantId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<StockResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<StockResponse>>> GetStockByVariantId([FromRoute] Guid variantId)
		{
			var response = await _stockService.GetStockByVariantIdAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("summary")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<InventorySummaryResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<InventorySummaryResponse>>> GetInventorySummary()
		{
			var response = await _stockService.GetInventorySummaryAsync();
			return HandleResponse(response);
		}
	}
}
