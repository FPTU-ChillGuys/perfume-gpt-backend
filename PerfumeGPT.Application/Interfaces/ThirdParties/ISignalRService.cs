using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISignalRService
	{
		Task NotifyNewOrderToStaff(Guid orderId, decimal totalAmount, string message);
       Task UpdateCustomerDisplayAsync(string sessionId, CartDisplayDto cartData);
	}
}
