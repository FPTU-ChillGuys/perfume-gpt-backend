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
			if (!saved) throw AppException.Internal("Tạo thông báo cho vai trò thất bại.");

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
				throw AppException.BadRequest("Bắt buộc cung cấp User ID.");

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
			if (!saved) throw AppException.Internal("Tạo thông báo cho người dùng thất bại.");

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
				return BaseResponse<string>.Fail("Bắt buộc cung cấp User ID.", ResponseErrorType.BadRequest);
			}

			var (exists, allowed, changed) = await _unitOfWork.Notifications.MarkAsReadAsync(id, userId, targetRole);

			if (!exists)
			{
				return BaseResponse<string>.Fail("Không tìm thấy thông báo.", ResponseErrorType.NotFound);
			}

			if (!allowed)
			{
				return BaseResponse<string>.Fail("Bạn không có quyền đánh dấu thông báo này là đã đọc.", ResponseErrorType.Forbidden);
			}

			if (changed)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved) throw AppException.Internal("Đánh dấu thông báo đã đọc thất bại.");
			}

			return BaseResponse<string>.Ok(id.ToString(), "Đã đánh dấu thông báo là đã đọc.");
		}

		public async Task<BaseResponse<PagedResult<NotificationListItemResponse>>> GetPagedAsync(GetPagedNotificationsRequest request)
		{
			if (request.UserId.HasValue && request.UserId.Value == Guid.Empty)
			{
				return BaseResponse<PagedResult<NotificationListItemResponse>>.Fail("User ID không hợp lệ.", ResponseErrorType.BadRequest);
			}

			var (items, totalCount) = await _unitOfWork.Notifications.GetPagedAsync(request);

			var pagedResult = new PagedResult<NotificationListItemResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<NotificationListItemResponse>>.Ok(
				pagedResult,
			   "Lấy danh sách thông báo thành công.");
		}

		public async Task<BaseResponse<string>> MarkAllAsReadAsync(Guid userId, string? targetRole)
		{
			if (userId == Guid.Empty)
			{
				return BaseResponse<string>.Fail("Bắt buộc cung cấp User ID.", ResponseErrorType.BadRequest);
			}

			var hasChanges = await _unitOfWork.Notifications.MarkAllAsReadAsync(userId, targetRole);

			if (hasChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved) throw AppException.Internal("Đánh dấu tất cả thông báo là đã đọc thất bại.");
			}

			return BaseResponse<string>.Ok("Đã đánh dấu tất cả thông báo là đã đọc.");
		}
	}
}
