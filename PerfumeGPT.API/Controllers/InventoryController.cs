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
		private readonly IBatchService _batchService;

		public InventoryController(IStockService stockService, IBatchService batchService)
		{
			_stockService = stockService;
			_batchService = batchService;
		}

		/// <summary>
		/// Get paginated stock inventory
		/// </summary>
		[HttpGet("stock")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<StockResponse>>>> GetInventory([FromQuery] GetInventoryRequest request)
		{
			var response = await _stockService.GetInventoryAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get stock for a specific variant
		/// </summary>
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

		/// <summary>
		/// Get paginated batches
		/// </summary>
		[HttpGet("batches")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BatchResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BatchResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<BatchResponse>>>> GetBatches([FromQuery] GetBatchesRequest request)
		{
			var response = await _batchService.GetBatchesAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get batches for a specific variant
		/// </summary>
		[HttpGet("batches/variant/{variantId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<List<BatchResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<BatchResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<BatchResponse>>>> GetBatchesByVariantId(Guid variantId)
		{
			var response = await _batchService.GetBatchesByVariantIdAsync(variantId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get batch by ID
		/// </summary>
		[HttpGet("batches/{batchId:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<BatchResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BatchResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<BatchResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BatchResponse>>> GetBatchById(Guid batchId)
		{
			var response = await _batchService.GetBatchByIdAsync(batchId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get inventory summary statistics
		/// </summary>
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
