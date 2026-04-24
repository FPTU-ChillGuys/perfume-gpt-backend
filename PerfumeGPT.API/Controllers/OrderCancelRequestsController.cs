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
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderCancelRequestResponse>>>> GetPagedRequests([FromQuery] GetPagedCancelRequestsRequest request)
		{
			var response = await _cancelRequestService.GetPagedRequestsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("my-requests")]
		[Authorize(Roles = "user")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<OrderCancelRequestResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<OrderCancelRequestResponse>>>> GetMyRequests([FromQuery] GetPagedCancelRequestsRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _cancelRequestService.GetPagedUserRequestsAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[Authorize(Roles = "user,admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<OrderCancelRequestResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<OrderCancelRequestResponse>>> GetRequestById([FromRoute] Guid id)
		{
			var requesterId = GetCurrentUserId();

			var isPrivilegedUser = User.IsInRole("admin") || User.IsInRole("staff");

			var response = await _cancelRequestService.GetRequestByIdAsync(id, requesterId, isPrivilegedUser);
			return HandleResponse(response);
		}


		[HttpPost("{id:guid}/process")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> ProcessRequest([FromRoute] Guid id, [FromBody] ProcessCancelRequest request)
		{
			var (staffId, userRole) = GetCurrentUserContext();
			if (userRole == null) return HandleResponse(BaseResponse<string>.Fail("Không có quyền truy cập.", ResponseErrorType.Unauthorized));

			var response = await _cancelRequestService.ProcessRequestAsync(id, staffId, userRole, request);
			return HandleResponse(response);
		}
	}
}
