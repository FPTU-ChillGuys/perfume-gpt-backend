using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
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

		[HttpPatch("{id:guid}/read")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> MarkAsRead([FromRoute] Guid id)
		{
			var response = await _notificationService.MarkAsReadAsync(id);
			return HandleResponse(response);
		}

		[HttpPatch("read-all")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> MarkAllAsRead()
		{
			var userId = GetCurrentUserId();
			var response = await _notificationService.MarkAllAsReadAsync(userId);
			return HandleResponse(response);
		}
	}
}
