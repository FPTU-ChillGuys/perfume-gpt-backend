using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OrderCancelRequests;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OrderCancelRequestsController : BaseApiController
	{
		private readonly IOrderCancelRequestService _cancelRequestService;

		public OrderCancelRequestsController(IOrderCancelRequestService cancelRequestService)
		{
			_cancelRequestService = cancelRequestService;
		}

		[HttpGet]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderCancelRequestResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderCancelRequestResponse>>>> GetPagedRequests([FromQuery] GetPagedCancelRequestsRequest request)
		{
			var response = await _cancelRequestService.GetPagedRequestsAsync(request);
			return HandleResponse(response);
		}


		[HttpPost("{id:guid}/process")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<string>>> ProcessRequest([FromRoute] Guid id, [FromBody] ProcessCancelRequest request)
		{
			var validation = ValidateRequestBody<ProcessCancelRequest>(request);
			if (validation != null) return validation;

			var staffId = GetCurrentUserId();
			var response = await _cancelRequestService.ProcessRequestAsync(id, staffId, request);
			return HandleResponse(response);
		}
	}
}
