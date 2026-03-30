namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface ISignalRService
	{
		Task NotifyNewOrderToStaff(Guid orderId, decimal totalAmount, string message);
	}
}
