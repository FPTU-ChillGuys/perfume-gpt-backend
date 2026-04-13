using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.DTOs.Responses.Notifications;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class NotificationService : INotificationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISignalRService _signalRService;

		public NotificationService(
			ISignalRService signalRService,
			IUnitOfWork unitOfWork)
		{
			_signalRService = signalRService;
			_unitOfWork = unitOfWork;
		}

		public async Task SendToRoleAsync(
			UserRole role,
			string title,
			string message,
			NotificationType type = NotificationType.Info,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null,
			string? metadataJson = null)
		{
			var payload = new Notification.NotificationPayload
			{
				Type = type,
				Title = title,
				Message = message,
				ReferenceId = referenceId,
				ReferenceType = referenceType,
				MetadataJson = metadataJson
			};

			var notification = Notification.CreateForRole(role.ToString().ToLowerInvariant(), payload);
			await _unitOfWork.Notifications.AddAsync(notification);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create role notification.");

			await _signalRService.SendNotificationToRoleAsync(role.ToString(), new
			{
				notificationId = notification.Id,
				title,
				message,
				type = type.ToString(),
				referenceId = notification.ReferenceId,
				referenceType = notification.ReferenceType,
				metadataJson = notification.MetadataJson,
				createdAt = notification.CreatedAt
			});
		}

		public async Task SendToUserAsync(
			Guid userId,
			string title,
			string message,
			NotificationType type = NotificationType.Info,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null,
			string? metadataJson = null)
		{
			if (userId == Guid.Empty)
				throw AppException.BadRequest("User ID is required.");

			var payload = new Notification.NotificationPayload
			{
				Type = type,
				Title = title,
				Message = message,
				ReferenceId = referenceId,
				ReferenceType = referenceType,
				MetadataJson = metadataJson
			};

			var notification = Notification.CreateForUser(userId, payload);
			await _unitOfWork.Notifications.AddAsync(notification);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create user notification.");

			await _signalRService.SendNotificationToUserAsync(userId, new
			{
				notificationId = notification.Id,
				title,
				message,
				type = type.ToString(),
				referenceId = notification.ReferenceId,
				referenceType = notification.ReferenceType,
				metadataJson = notification.MetadataJson,
				createdAt = notification.CreatedAt
			});
		}

		public async Task<BaseResponse<string>> MarkAsReadAsync(Guid id, Guid userId, string? targetRole)
		{
			if (userId == Guid.Empty)
			{
				return BaseResponse<string>.Fail("User ID is required.", ResponseErrorType.BadRequest);
			}

			var (exists, allowed, changed) = await _unitOfWork.Notifications.MarkAsReadAsync(id, userId, targetRole);

			if (!exists)
			{
				return BaseResponse<string>.Fail("Notification not found.", ResponseErrorType.NotFound);
			}

			if (!allowed)
			{
				return BaseResponse<string>.Fail("You do not have permission to mark this notification as read.", ResponseErrorType.Forbidden);
			}

			if (changed)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved) throw AppException.Internal("Failed to mark notification as read.");
			}

			return BaseResponse<string>.Ok(id.ToString(), "Notification marked as read.");
		}

		public async Task<BaseResponse<PagedResult<NotificationListItemResponse>>> GetPagedAsync(GetPagedNotificationsRequest request)
		{
			if (request.UserId.HasValue && request.UserId.Value == Guid.Empty)
			{
				return BaseResponse<PagedResult<NotificationListItemResponse>>.Fail("User ID is invalid.", ResponseErrorType.BadRequest);
			}

			var (items, totalCount) = await _unitOfWork.Notifications.GetPagedAsync(request);

			var pagedResult = new PagedResult<NotificationListItemResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<NotificationListItemResponse>>.Ok(
				pagedResult,
				"Notifications retrieved successfully.");
		}

		public async Task<BaseResponse<string>> MarkAllAsReadAsync(Guid userId, string? targetRole)
		{
			if (userId == Guid.Empty)
			{
				return BaseResponse<string>.Fail("User ID is required.", ResponseErrorType.BadRequest);
			}

			var hasChanges = await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, targetRole);

			if (hasChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved) throw AppException.Internal("Failed to mark all notifications as read.");
			}

			return BaseResponse<string>.Ok("All notifications were marked as read.");
		}
	}
}
