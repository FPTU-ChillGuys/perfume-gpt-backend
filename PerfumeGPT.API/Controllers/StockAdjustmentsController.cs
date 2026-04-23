using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.StockAdjustments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StockAdjustmentsController : BaseApiController
	{
		private readonly IStockAdjustmentService _stockAdjustmentService;

		public StockAdjustmentsController(IStockAdjustmentService stockAdjustmentService)
		{
			_stockAdjustmentService = stockAdjustmentService;
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateStockAdjustment([FromBody] CreateStockAdjustmentRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _stockAdjustmentService.CreateStockAdjustmentAsync(request, userId);
			return HandleResponse(response);
		}

		[HttpPost("{adjustmentId:guid}/verify")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> VerifyStockAdjustment([FromRoute] Guid adjustmentId, [FromBody] VerifyStockAdjustmentRequest request)
		{
			var verifiedByUserId = GetCurrentUserId();
			var response = await _stockAdjustmentService.VerifyStockAdjustmentAsync(adjustmentId, request, verifiedByUserId);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<StockAdjustmentResponse>>> GetStockAdjustmentById([FromRoute] Guid id)
		{
			var response = await _stockAdjustmentService.GetStockAdjustmentByIdAsync(id);
			return HandleResponse(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockAdjustmentListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<StockAdjustmentListItem>>>> GetPagedStockAdjustments([FromQuery] GetPagedStockAdjustmentsRequest request)
		{
			var response = await _stockAdjustmentService.GetPagedStockAdjustmentsAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/status")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAdjustmentStatus([FromRoute] Guid id, [FromBody] UpdateStockAdjustmentStatusRequest request)
		{
			var response = await _stockAdjustmentService.UpdateAdjustmentStatusAsync(id, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteStockAdjustment([FromRoute] Guid id)
		{
			var response = await _stockAdjustmentService.DeleteStockAdjustmentAsync(id);
			return HandleResponse(response);
		}
	}
}
