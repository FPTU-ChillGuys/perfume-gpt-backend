using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISignalRService
	{
		Task NotifyNewOrderToStaff(Guid orderId, decimal totalAmount, string message);
		Task UpdateCustomerDisplayAsync(string sessionId, CartDisplayDto cartData);
		Task NotifyPosPaymentCompletedAsync(string sessionId, PosPaymentCompletedDto paymentData);
	}
}
