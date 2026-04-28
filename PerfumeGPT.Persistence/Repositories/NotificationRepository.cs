using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

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

			var hasUserFilter = request.UserId.HasValue && request.UserId.Value != Guid.Empty;
			var hasRoleFilter = !string.IsNullOrWhiteSpace(request.TargetRole);
			var userId = request.UserId ?? Guid.Empty;

			Expression<Func<Notification, bool>> filter = x => true;
			if (hasUserFilter || hasRoleFilter)
			{
				Expression<Func<Notification, bool>> audienceFilter = x => false;

				if (hasUserFilter)
				{
					audienceFilter = audienceFilter.OrElse(n => n.UserId == userId);
				}

				if (hasRoleFilter)
				{
					var normalizedRole = request.TargetRole!.Trim().ToLowerInvariant();
					audienceFilter = audienceFilter.OrElse(n => n.TargetRole != null && n.TargetRole.ToLower() == normalizedRole);
				}

				filter = filter.AndAlso(audienceFilter);
			}

			if (request.IsRead.HasValue)
			{
				if (hasUserFilter)
				{
					var isReadFilter = request.IsRead.Value;
					filter = filter.AndAlso(n => n.UserId.HasValue
						? n.IsRead == isReadFilter
						: _context.UserNotificationReads.Any(unr => unr.NotificationId == n.Id && unr.UserId == userId) == isReadFilter);
				}
				else
				{
					var isReadFilter = request.IsRead.Value;
					filter = filter.AndAlso(n => n.IsRead == isReadFilter);
				}
			}

			query = query.Where(filter);
			var totalCount = await query.CountAsync();
			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(Notification.Title),
				nameof(Notification.Type),
				nameof(Notification.CreatedAt),
				nameof(Notification.IsRead)
			};

			var sortBy = request.SortBy?.Trim();
			sortBy = !string.IsNullOrWhiteSpace(sortBy)
				? (sortBy.Length == 1
					? char.ToUpper(sortBy[0]).ToString()
					: char.ToUpper(sortBy[0]) + sortBy.Substring(1))
				: null;

			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(n => n.CreatedAt);

			List<NotificationListItemResponse> items;

			if (hasUserFilter)
			{
				items = await sortedQuery
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
						IsRead = n.UserId.HasValue
							? n.IsRead
							: _context.UserNotificationReads.Any(unr => unr.NotificationId == n.Id && unr.UserId == userId),
						CreatedAt = n.CreatedAt
					})
					.ToListAsync();
			}
			else
			{
				items = await sortedQuery
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
			}

			return (items, totalCount);
		}

		public async Task<(bool Exists, bool Allowed, bool Changed)> MarkAsReadAsync(Guid notificationId, Guid userId, string? targetRole)
		{
			var notification = await _context.Notifications
				.FirstOrDefaultAsync(n => n.Id == notificationId);

			if (notification == null)
				return (false, false, false);

			if (notification.UserId.HasValue)
			{
				if (notification.UserId.Value != userId)
					return (true, false, false);

				if (notification.IsRead)
					return (true, true, false);

				notification.MarkAsRead();
				_context.Notifications.Update(notification);
				return (true, true, true);
			}

			if (string.IsNullOrWhiteSpace(targetRole))
				return (true, false, false);

			var normalizedRole = targetRole.Trim().ToLowerInvariant();
			if (!string.Equals(notification.TargetRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
				return (true, false, false);

			var alreadyRead = await _context.UserNotificationReads
				.AnyAsync(unr => unr.NotificationId == notificationId && unr.UserId == userId);

			if (alreadyRead)
				return (true, true, false);

			await _context.UserNotificationReads.AddAsync(UserNotificationRead.Create(userId, notificationId));

			return (true, true, true);
		}

		public async Task<bool> MarkAllAsReadAsync(Guid userId, string? targetRole)
		{
			var hasChanges = false;

			var unreadPersonal = await _context.Notifications
				.Where(n => n.UserId == userId && !n.IsRead)
				.ToListAsync();

			if (unreadPersonal.Count > 0)
			{
				foreach (var notification in unreadPersonal)
				{
					notification.MarkAsRead();
					_context.Notifications.Update(notification);
				}

				hasChanges = true;
			}

			if (string.IsNullOrWhiteSpace(targetRole))
				return hasChanges;

			var normalizedRole = targetRole.Trim().ToLowerInvariant();
			var unreadBroadcastIds = await _context.Notifications
				.AsNoTracking()
				.Where(n => !n.UserId.HasValue && n.TargetRole != null && n.TargetRole.ToLower() == normalizedRole)
				.Where(n => !_context.UserNotificationReads.Any(unr => unr.NotificationId == n.Id && unr.UserId == userId))
				.Select(n => n.Id)
				.ToListAsync();

			if (unreadBroadcastIds.Count == 0)
				return hasChanges;

			var readRecords = unreadBroadcastIds
				.Select(notificationId => UserNotificationRead.Create(userId, notificationId))
				.ToList();

			await _context.UserNotificationReads.AddRangeAsync(readRecords);

			return true;
		}
	}
}
