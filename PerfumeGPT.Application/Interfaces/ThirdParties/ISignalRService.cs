using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISignalRService
	{
		Task SendNotificationToRoleAsync(string role, object payload);
		Task SendNotificationToUserAsync(Guid userId, object payload);
		Task UpdateCustomerDisplayAsync(string sessionId, CartDisplayDto cartData);
		Task NotifyPosPaymentCompletedAsync(string sessionId, PosPaymentCompletedDto paymentData);
		Task NotifyPosPaymentFailedAsync(string sessionId, PosPaymentCompletedDto paymentData);
		Task NotifyPosPaymentLinkUpdatedAsync(string sessionId, PosPaymentLinkDto paymentData);
	}
}
