using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Notifications;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class FcmNotificationService : IFcmNotificationService
	{
		private readonly ILogger<FcmNotificationService> _logger;

		public FcmNotificationService(ILogger<FcmNotificationService> logger)
		{
			_logger = logger;
		}

		public async Task<bool> SendToDeviceAsync(SendPushNotificationRequest request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.DeviceToken))
			{
				_logger.LogWarning("Skip sending FCM because request or device token is invalid.");
				return false;
			}

			var message = new Message()
			{
				Token = request.DeviceToken,
				Notification = new Notification()
				{
					Title = request.Title,
					Body = request.Body,
				},
				Data = request.Data, // Dùng để truyền dữ liệu ngầm (VD: orderId để app mở đúng màn hình)

				// Cấu hình riêng cho iOS (Tùy chọn)
				Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } },
				// Cấu hình riêng cho Android (Tùy chọn)
				Android = new AndroidConfig { Priority = Priority.High }
			};

			try
			{
				var firebaseMessaging = FirebaseMessaging.DefaultInstance;
				if (firebaseMessaging == null)
				{
					_logger.LogWarning("Skip sending FCM because FirebaseMessaging.DefaultInstance is not initialized.");
					return false;
				}

				string response = await firebaseMessaging.SendAsync(message);
				return true;
			}
			catch (FirebaseMessagingException ex)
			{
				// BẮT EDGE CASE QUAN TRỌNG: Token đã chết
				if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
					ex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
				{
					// TODO: Token này đã bị vô hiệu hóa (User gỡ app hoặc clear data).
					// Bạn cần gọi IUnitOfWork để xóa token này khỏi database để lần sau không gửi nhầm nữa.
				}
				return false;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Unexpected error while sending FCM.");
				return false;
			}
		}
	}
}
