using PerfumeGPT.Application.DTOs.Requests.Notifications;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IFcmNotificationService
	{
		Task<bool> SendToDeviceAsync(SendPushNotificationRequest request);
	}
}
