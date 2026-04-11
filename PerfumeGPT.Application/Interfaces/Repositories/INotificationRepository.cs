using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface INotificationRepository : IGenericRepository<Notification>
	{
		Task<(List<NotificationListItemResponse> Items, int TotalCount)> GetPagedAsync(GetPagedNotificationsRequest request);
		Task<bool> MarkAllAsReadAsync(Guid userId);
	}
}
