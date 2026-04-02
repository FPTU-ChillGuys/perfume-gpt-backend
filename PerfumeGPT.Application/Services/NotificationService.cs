using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

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

		public async Task CreateNewOrderNotificationAsync(Guid orderId, decimal totalAmount)
		{
			var message = $"New order {orderId} was created with total amount {totalAmount:N0}.";

			await _signalRService.NotifyNewOrderToStaff(orderId, totalAmount, message);
		}

		public async Task<BaseResponse<string>> MarkAsReadAsync(Guid id)
		{
			var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
			if (notification == null)
			{
				return BaseResponse<string>.Fail("Notification not found.", ResponseErrorType.NotFound);
			}

			notification.MarkAsRead();
			_unitOfWork.Notifications.Update(notification);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to mark notification as read.");

			return BaseResponse<string>.Ok(id.ToString(), "Notification marked as read.");
		}

		public async Task<BaseResponse<string>> MarkAllAsReadAsync(Guid userId)
		{
			await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to mark all notifications as read.");

			return BaseResponse<string>.Ok("All notifications were marked as read.");
		}
	}
}
