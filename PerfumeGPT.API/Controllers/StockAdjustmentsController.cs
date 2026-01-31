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

		/// <summary>
		/// Create a new stock adjustment
		/// </summary>
		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateStockAdjustment([FromBody] CreateStockAdjustmentRequest request)
		{
			var validation = ValidateRequestBody<CreateStockAdjustmentRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _stockAdjustmentService.CreateStockAdjustmentAsync(request, userId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Verify stock adjustment and apply stock changes
		/// </summary>
		[HttpPost("{adjustmentId:guid}/verify")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> VerifyStockAdjustment([FromRoute] Guid adjustmentId, [FromBody] VerifyStockAdjustmentRequest request)
		{
			var validation = ValidateRequestBody<VerifyStockAdjustmentRequest>(request);
			if (validation != null) return validation;

			var verifiedByUserId = GetCurrentUserId();
			var response = await _stockAdjustmentService.VerifyStockAdjustmentAsync(adjustmentId, request, verifiedByUserId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get stock adjustment by ID
		/// </summary>
		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<StockAdjustmentResponse>>> GetStockAdjustmentById(Guid id)
		{
			var response = await _stockAdjustmentService.GetStockAdjustmentByIdAsync(id);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get paged list of stock adjustments
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockAdjustmentListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockAdjustmentListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<StockAdjustmentListItem>>>> GetPagedStockAdjustments([FromQuery] GetPagedStockAdjustmentsRequest request)
		{
			var response = await _stockAdjustmentService.GetPagedStockAdjustmentsAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Update stock adjustment status
		/// </summary>
		[HttpPut("{id:guid}/status")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAdjustmentStatus([FromRoute] Guid id, [FromBody] UpdateStockAdjustmentStatusRequest request)
		{
			var validation = ValidateRequestBody<UpdateStockAdjustmentStatusRequest>(request);
			if (validation != null) return validation;

			var response = await _stockAdjustmentService.UpdateAdjustmentStatusAsync(id, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Delete a stock adjustment
		/// </summary>
		[HttpDelete("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteStockAdjustment(Guid id)
		{
			var response = await _stockAdjustmentService.DeleteStockAdjustmentAsync(id);
			return HandleResponse(response);
		}
	}
}
