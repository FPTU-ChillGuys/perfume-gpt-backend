using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Notifications;

using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface INotificationService
	{
		Task SendToRoleAsync(UserRole role, string title, string message, NotificationType type = NotificationType.Info, Guid? referenceId = null, NotifiReferecneType? referenceType = null, string? metadataJson = null);
		Task SendToUserAsync(Guid userId, string title, string message, NotificationType type = NotificationType.Info, Guid? referenceId = null, NotifiReferecneType? referenceType = null, string? metadataJson = null);
		Task<BaseResponse<PagedResult<NotificationListItemResponse>>> GetPagedAsync(GetPagedNotificationsRequest request);
		Task<BaseResponse<string>> MarkAsReadAsync(Guid id, Guid userId, string? targetRole);
		Task<BaseResponse<string>> MarkAllAsReadAsync(Guid userId, string? targetRole);
	}
}
