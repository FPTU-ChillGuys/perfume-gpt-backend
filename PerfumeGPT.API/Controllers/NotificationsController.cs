using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Interfaces.Services;

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
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<NotificationListItemResponse>>>> GetPaged([FromQuery] GetPagedNotificationsRequest request)
		{
			var (currentUserId, currentRole) = GetCurrentUserContext();

			// Nếu request không truyền UserId (hoặc empty) → fallback về current user
			var effectiveUserId = request.UserId is { } uid && uid != Guid.Empty
				? uid
				: (currentUserId == Guid.Empty ? (Guid?)null : currentUserId);

			// Nếu request không truyền TargetRole, và đang query cho chính mình → dùng role hiện tại
			var effectiveRole = !string.IsNullOrWhiteSpace(request.TargetRole)
				? request.TargetRole
				: (effectiveUserId.HasValue && effectiveUserId.Value == currentUserId ? currentRole : null);

			var response = await _notificationService.GetPagedAsync(request with
			{
				UserId = effectiveUserId,
				TargetRole = effectiveRole
			});

			return HandleResponse(response);
		}

		[HttpPatch("{id:guid}/read")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> MarkAsRead([FromRoute] Guid id)
		{
			var (userId, role) = GetCurrentUserContext();

			var response = await _notificationService.MarkAsReadAsync(id, userId, role);
			return HandleResponse(response);
		}

		[HttpPatch("read-all")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> MarkAllAsRead()
		{
			var (userId, role) = GetCurrentUserContext();

			var response = await _notificationService.MarkAllAsReadAsync(userId, role);
			return HandleResponse(response);
		}
	}
}
