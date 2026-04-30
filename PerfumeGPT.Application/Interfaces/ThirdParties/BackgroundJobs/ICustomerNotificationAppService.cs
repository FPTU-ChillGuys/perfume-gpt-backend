using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface ICustomerNotificationAppService
	{
		Task NotifyOrderPreparingAsync(Guid orderId, string orderCode, Guid customerId);
		Task NotifyCustomerWithFcmAsync(
			Guid customerId,
			string title,
			string message,
			NotificationType type,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null);
	}
}
