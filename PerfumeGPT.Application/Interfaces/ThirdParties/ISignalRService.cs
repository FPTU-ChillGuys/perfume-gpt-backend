namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISignalRService
	{
		Task NotifyNewOrderToStaff(string orderId, decimal totalAmount);
		Task NotifyProductCreated(Guid id);
	}
}
