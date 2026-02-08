using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class InventoryController : BaseApiController
	{
		private readonly IStockService _stockService;
		private readonly IBatchService _batchService;

		public InventoryController(IStockService stockService, IBatchService batchService)
		{
			_stockService = stockService;
			_batchService = batchService;
		}

		[HttpGet("stock")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<StockResponse>>>> GetInventory([FromQuery] GetPagedInventoryRequest request)
		{
			var response = await _stockService.GetInventoryAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("stock/variant/{variantId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<StockResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<StockResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<StockResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<StockResponse>>> GetStockByVariantId(Guid variantId)
		{
			var response = await _stockService.GetStockByVariantIdAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("batches")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BatchDetailResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BatchDetailResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<BatchDetailResponse>>>> GetBatches([FromQuery] GetBatchesRequest request)
		{
			var response = await _batchService.GetBatchesAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("batches/variant/{variantId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<List<BatchDetailResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<BatchDetailResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<BatchDetailResponse>>>> GetBatchesByVariantId(Guid variantId)
		{
			var response = await _batchService.GetBatchesByVariantIdAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("batches/{batchId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<BatchDetailResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BatchDetailResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<BatchDetailResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BatchDetailResponse>>> GetBatchById(Guid batchId)
		{
			var response = await _batchService.GetBatchByIdAsync(batchId);
			return HandleResponse(response);
		}

		[HttpGet("summary")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<InventorySummaryResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<InventorySummaryResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<InventorySummaryResponse>>> GetInventorySummary()
		{
			var response = await _stockService.GetInventorySummaryAsync();
			return HandleResponse(response);
		}
	}
}
