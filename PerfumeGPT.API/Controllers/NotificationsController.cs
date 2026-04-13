using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Interfaces.Services;
using System.Security.Claims;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class NotificationsController : BaseApiController
	{
		private readonly INotificationService _notificationService;

		public NotificationsController(INotificationService notificationService)
		{
			_notificationService = notificationService;
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<NotificationListItemResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<PagedResult<NotificationListItemResponse>>>> GetPaged([FromQuery] GetPagedNotificationsRequest request)
		{
			var effectiveRequest = request;
			var currentUserId = GetCurrentUserId();
			var currentRole = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role);

			Guid? effectiveUserId = effectiveRequest.UserId;
			var effectiveRole = effectiveRequest.TargetRole;

			if (!effectiveUserId.HasValue || effectiveUserId.Value == Guid.Empty)
			{
				effectiveUserId = currentUserId == Guid.Empty ? null : currentUserId;
			}

			if (string.IsNullOrWhiteSpace(effectiveRole)
				&& effectiveUserId.HasValue
				&& currentUserId != Guid.Empty
				&& effectiveUserId.Value == currentUserId)
			{
				effectiveRole = currentRole;
			}

			effectiveRequest = effectiveRequest with
			{
				UserId = effectiveUserId,
				TargetRole = effectiveRole
			};

			var response = await _notificationService.GetPagedAsync(effectiveRequest);
			return HandleResponse(response);
		}

		[HttpPatch("{id:guid}/read")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> MarkAsRead([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var role = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role);
			var response = await _notificationService.MarkAsReadAsync(id, userId, role);
			return HandleResponse(response);
		}

		[HttpPatch("read-all")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> MarkAllAsRead()
		{
			var userId = GetCurrentUserId();
			var role = User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role);
			var response = await _notificationService.MarkAllAsReadAsync(userId, role);
			return HandleResponse(response);
		}
	}
}
