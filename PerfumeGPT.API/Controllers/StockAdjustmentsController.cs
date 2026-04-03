using FluentValidation;
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
		private readonly IValidator<CreateStockAdjustmentRequest> _createValidator;
		private readonly IValidator<VerifyStockAdjustmentRequest> _verifyValidator;
		private readonly IValidator<UpdateStockAdjustmentStatusRequest> _updateStatusValidator;

		public StockAdjustmentsController(
			IStockAdjustmentService stockAdjustmentService,
			IValidator<CreateStockAdjustmentRequest> createValidator,
			IValidator<VerifyStockAdjustmentRequest> verifyValidator,
			IValidator<UpdateStockAdjustmentStatusRequest> updateStatusValidator)
		{
			_stockAdjustmentService = stockAdjustmentService;
			_createValidator = createValidator;
			_verifyValidator = verifyValidator;
			_updateStatusValidator = updateStatusValidator;
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateStockAdjustment([FromBody] CreateStockAdjustmentRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _stockAdjustmentService.CreateStockAdjustmentAsync(request, userId);
			return HandleResponse(response);
		}

		[HttpPost("{adjustmentId:guid}/verify")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> VerifyStockAdjustment([FromRoute] Guid adjustmentId, [FromBody] VerifyStockAdjustmentRequest request)
		{
			var validation = await ValidateRequestAsync(_verifyValidator, request);
			if (validation != null) return validation;

			var verifiedByUserId = GetCurrentUserId();
			var response = await _stockAdjustmentService.VerifyStockAdjustmentAsync(adjustmentId, request, verifiedByUserId);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<StockAdjustmentResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<StockAdjustmentResponse>>> GetStockAdjustmentById([FromRoute] Guid id)
		{
			var response = await _stockAdjustmentService.GetStockAdjustmentByIdAsync(id);
			return HandleResponse(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockAdjustmentListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<StockAdjustmentListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<StockAdjustmentListItem>>>> GetPagedStockAdjustments([FromQuery] GetPagedStockAdjustmentsRequest request)
		{
			var response = await _stockAdjustmentService.GetPagedStockAdjustmentsAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/status")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAdjustmentStatus([FromRoute] Guid id, [FromBody] UpdateStockAdjustmentStatusRequest request)
		{
			var validation = await ValidateRequestAsync(_updateStatusValidator, request);
			if (validation != null) return validation;

			var response = await _stockAdjustmentService.UpdateAdjustmentStatusAsync(id, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteStockAdjustment([FromRoute] Guid id)
		{
			var response = await _stockAdjustmentService.DeleteStockAdjustmentAsync(id);
			return HandleResponse(response);
		}
	}
}
