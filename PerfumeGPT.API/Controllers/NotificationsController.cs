using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class NotificationsController : BaseApiController
	{
		private readonly INotificationService _notificationService;
		private readonly IFcmNotificationService _fcmService;

		public NotificationsController(INotificationService notificationService, IFcmNotificationService fcmService)
		{
			_notificationService = notificationService;
			_fcmService = fcmService;
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

		[HttpPost("test-send")]
		[AllowAnonymous] // Cho phép gọi mà không cần auth, vì đây là endpoint test
		public async Task<IActionResult> SendTestNotification([FromBody] SendPushNotificationRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.DeviceToken))
			{
				return BadRequest(BaseResponse<string>.Fail("Device Token không được để trống.", ResponseErrorType.BadRequest));
			}

			// Gọi service bắn FCM
			bool isSuccess = await _fcmService.SendToDeviceAsync(request);
			if (isSuccess)
			{
				return Ok(BaseResponse<string>.Ok("Đã gửi thông báo thành công đến Google FCM."));
			}
			else
			{
				// Trả về 400 nếu Token sai, đã hết hạn, hoặc bị Google từ chối
				return BadRequest(BaseResponse<string>.Fail("Gửi thất bại. Device Token có thể không hợp lệ hoặc đã hết hạn.", ResponseErrorType.BadRequest));
			}
		}
	}
}
