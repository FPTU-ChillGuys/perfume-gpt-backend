using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrderReturnRequestsController : BaseApiController
	{
		private readonly IOrderReturnRequestService _returnRequestService;

		public OrderReturnRequestsController(IOrderReturnRequestService returnRequestService)
		{
			_returnRequestService = returnRequestService;
		}

		[HttpPost]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> CreateReturnRequest([FromBody] CreateReturnRequestDto request)
		{
			var validation = ValidateRequestBody<CreateReturnRequestDto>(request);
			if (validation != null) return validation;

			var customerId = GetCurrentUserId();
			var response = await _returnRequestService.CreateReturnRequestAsync(customerId, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/review")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> ProcessInitialRequest([FromRoute] Guid id, [FromBody] ProcessInitialReturnDto request)
		{
			var validation = ValidateRequestBody<ProcessInitialReturnDto>(request);
			if (validation != null) return validation;

			var processedById = GetCurrentUserId();
			var response = await _returnRequestService.ProcessInitialRequestAsync(processedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/start-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> StartInspection([FromRoute] Guid id, [FromBody] StartInspectionDto request)
		{
			var validation = ValidateRequestBody<StartInspectionDto>(request);
			if (validation != null) return validation;

			var inspectedById = GetCurrentUserId();
			var response = await _returnRequestService.StartInspectionAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/complete-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> RecordInspectionResult([FromRoute] Guid id, [FromBody] RecordInspectionDto request)
		{
			var validation = ValidateRequestBody<RecordInspectionDto>(request);
			if (validation != null) return validation;

			var inspectedById = GetCurrentUserId();
			var response = await _returnRequestService.RecordInspectionResultAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/fail-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> RejectAfterInspection([FromRoute] Guid id, [FromBody] RejectInspectionDto request)
		{
			var validation = ValidateRequestBody<RejectInspectionDto>(request);
			if (validation != null) return validation;

			var inspectedById = GetCurrentUserId();
			var response = await _returnRequestService.RejectAfterInspectionAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/refund")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> ProcessRefund([FromRoute] Guid id)
		{
			var financeAdminId = GetCurrentUserId();
			var response = await _returnRequestService.ProcessRefundAsync(financeAdminId, id);
			return HandleResponse(response);
		}
	}
}
