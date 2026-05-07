using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class CustomerNotificationJob : ICustomerNotificationAppService
	{
		private readonly INotificationService _notificationService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IFcmNotificationService _fcmNotificationService;

		public CustomerNotificationJob(
			INotificationService notificationService,
			IUnitOfWork unitOfWork,
			IFcmNotificationService fcmNotificationService)
		{
			_notificationService = notificationService;
			_unitOfWork = unitOfWork;
			_fcmNotificationService = fcmNotificationService;
		}

		public async Task NotifyOrderPreparingAsync(Guid orderId, string orderCode, Guid customerId)
		{
			var preparingTitle = "Đơn hàng đã được xác nhận";
			var existedPreparingNotification = await _unitOfWork.Notifications.AnyAsync(n =>
				n.UserId == customerId
				&& n.ReferenceId == orderId
				&& n.ReferenceType == NotifiReferecneType.Order
				&& n.Title == preparingTitle);

			if (existedPreparingNotification)
			{
				return;
			}

			await NotifyCustomerWithFcmAsync(
				customerId,
				preparingTitle,
				$"Đơn hàng #{orderCode} của bạn đã được xác nhận và đang xử lý.",
				NotificationType.Info,
				orderId,
				NotifiReferecneType.Order);
		}

		public async Task NotifyCustomerWithFcmAsync(
			Guid customerId,
			string title,
			string message,
			NotificationType type,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null)
		{
			await _notificationService.SendToUserAsync(
				customerId,
				title,
				message,
				type,
				referenceId: referenceId,
				referenceType: referenceType);

			var userDevices = (await _unitOfWork.UserDeviceTokens.GetAllAsync(t => t.UserId == customerId)).ToList();
			if (userDevices.Count == 0)
			{
				return;
			}

			var deadTokens = new List<UserDeviceToken>();
			foreach (var device in userDevices)
			{
				var fcmMessage = new SendPushNotificationRequest
				{
					DeviceToken = device.Token,
					Title = title,
					Body = message,
					Data = new Dictionary<string, string>
					{
						{ "orderId", referenceId?.ToString() ?? string.Empty },
						{ "type", "OrderStatusUpdate" }
					}
				};

				var isSuccess = await _fcmNotificationService.SendToDeviceAsync(fcmMessage);
				if (!isSuccess)
				{
					deadTokens.Add(device);
				}
			}

			if (deadTokens.Count > 0)
			{
				_unitOfWork.UserDeviceTokens.RemoveRange(deadTokens);
				await _unitOfWork.SaveChangesAsync();
			}
		}
	}
}
