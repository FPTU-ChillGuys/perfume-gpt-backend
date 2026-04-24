using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Batches;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BatchesController : BaseApiController
	{
		private readonly IBatchService _batchService;

		public BatchesController(IBatchService batchService)
		{
			_batchService = batchService;
		}

		[HttpGet]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<BatchDetailResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<BatchDetailResponse>>>> GetBatches([FromQuery] GetBatchesRequest request)
		{
			var response = await _batchService.GetBatchesAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<BatchDetailResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BatchDetailResponse>>> GetBatchById([FromRoute] Guid id)
		{
			var response = await _batchService.GetBatchByIdAsync(id);
			return HandleResponse(response);
		}

		[HttpGet("lookup")]
		public async Task<ActionResult<BaseResponse<List<BatchLookupResponse>>>> GetBatchLookup()
		{
			var response = await _batchService.GetBatchLookupAsync();
			return HandleResponse(response);
		}
	}
}
