using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
	{
		public NotificationRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<NotificationListItemResponse> Items, int TotalCount)> GetPagedAsync(GetPagedNotificationsRequest request)
		{
			var query = _context.Notifications
				.AsNoTracking()
				.AsQueryable();

			var hasUserFilter = request.UserId.HasValue;
			var hasRoleFilter = !string.IsNullOrWhiteSpace(request.TargetRole);

			if (hasUserFilter && hasRoleFilter)
			{
				var normalizedRole = request.TargetRole!.Trim().ToLower();
				var userId = request.UserId!.Value;
				query = query.Where(n => n.UserId == userId || (n.TargetRole != null && n.TargetRole.ToLower() == normalizedRole));
			}
			else if (hasUserFilter)
			{
				var userId = request.UserId!.Value;
				query = query.Where(n => n.UserId == userId);
			}
			else if (hasRoleFilter)
			{
				var normalizedRole = request.TargetRole!.Trim().ToLower();
				query = query.Where(n => n.TargetRole != null && n.TargetRole.ToLower() == normalizedRole);
			}

			if (request.IsRead.HasValue)
				query = query.Where(n => n.IsRead == request.IsRead.Value);

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(n => n.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(n => new NotificationListItemResponse
				{
					Id = n.Id,
					UserId = n.UserId,
					TargetRole = n.TargetRole,
					Title = n.Title,
					Message = n.Message,
					Type = n.Type,
					ReferenceId = n.ReferenceId,
					ReferenceType = n.ReferenceType,
					MetadataJson = n.MetadataJson,
					IsRead = n.IsRead,
					CreatedAt = n.CreatedAt
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<bool> MarkAllAsReadAsync(Guid userId)
		{
			var unreadNotifications = await _context.Notifications
				  .Where(n => n.UserId == userId && !n.IsRead)
				  .ToListAsync();

			if (unreadNotifications.Count == 0)
				return false;

			foreach (var notification in unreadNotifications)
			{
				notification.MarkAsRead();
				_context.Notifications.Update(notification);
			}

			return true;
		}
	}
}
