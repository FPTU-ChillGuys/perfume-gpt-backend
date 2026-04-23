using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrderReturnRequestsController : BaseApiController
	{
		private readonly IOrderReturnRequestService _returnRequestService;
		private readonly IMediaService _mediaService;

		public OrderReturnRequestsController(
			IOrderReturnRequestService returnRequestService,
			IMediaService mediaService)
		{
			_returnRequestService = returnRequestService;
			_mediaService = mediaService;
		}

		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderReturnRequestResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderReturnRequestResponse>>>> GetPagedReturnRequests([FromQuery] GetPagedReturnRequestsRequest request)
		{
			var response = await _returnRequestService.GetPagedReturnRequestsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("my-requests")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderReturnRequestResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderReturnRequestResponse>>>> GetMyReturnRequests([FromQuery] GetPagedUserReturnRequestsRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _returnRequestService.GetPagedUserReturnRequestsAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[Authorize(Roles = "user,admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<OrderReturnRequestResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<OrderReturnRequestResponse>>> GetReturnRequestById([FromRoute] Guid id)
		{
			var requesterId = GetCurrentUserId();

			var isPrivilegedUser = User.IsInRole("admin") || User.IsInRole("staff");

			var response = await _returnRequestService.GetReturnRequestByIdAsync(id, requesterId, isPrivilegedUser);
			return HandleResponse(response);
		}

		[HttpPost]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateReturnRequest([FromBody] CreateReturnRequestDto request)
		{
			var customerId = GetCurrentUserId();

			var response = await _returnRequestService.CreateReturnRequestAsync(customerId, request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateReturnRequest([FromRoute] Guid id, [FromBody] UpdateReturnRequestDto request)
		{
			var customerId = GetCurrentUserId();

			var response = await _returnRequestService.UpdateReturnRequestAsync(customerId, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/cancel")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CancelReturnRequest([FromRoute] Guid id)
		{
			var customerId = GetCurrentUserId();

			var response = await _returnRequestService.CancelReturnRequestByCustomerAsync(customerId, id);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/review")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ProcessInitialRequest([FromRoute] Guid id, [FromBody] ProcessInitialReturnDto request)
		{
			var processedById = GetCurrentUserId();

			var response = await _returnRequestService.ProcessInitialRequestAsync(processedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/start-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> StartInspection([FromRoute] Guid id, [FromBody] StartInspectionDto request)
		{
			var inspectedById = GetCurrentUserId();

			var response = await _returnRequestService.StartInspectionAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/complete-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> RecordInspectionResult([FromRoute] Guid id, [FromBody] RecordInspectionDto request)
		{
			var inspectedById = GetCurrentUserId();

			var response = await _returnRequestService.RecordInspectionResultAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/fail-inspection")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> RejectAfterInspection([FromRoute] Guid id, [FromBody] RejectInspectionDto request)
		{
			var inspectedById = GetCurrentUserId();

			var response = await _returnRequestService.RejectAfterInspectionAsync(inspectedById, id, request);
			return HandleResponse(response);
		}

		[HttpPost("{id:guid}/refund")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ProcessRefund([FromRoute] Guid id, [FromBody] ProcessRefundRequest request)
		{
			var financeAdminId = GetCurrentUserId();

			var response = await _returnRequestService.ProcessRefundAsync(financeAdminId, id, request);
			return HandleResponse(response);
		}

		[HttpPost("videos/temporary")]
		[Authorize(Roles = "user")]
		[RequestSizeLimit(104_857_600)]
		[RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryVideos([FromForm] OrderReturnRequestUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _mediaService.UploadOrderReturnRequestTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}
	}
}
