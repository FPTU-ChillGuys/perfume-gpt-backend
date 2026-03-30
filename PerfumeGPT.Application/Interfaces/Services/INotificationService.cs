using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface INotificationService
	{
		Task CreateNewOrderNotificationAsync(Guid orderId, decimal totalAmount);
		Task<BaseResponse<string>> MarkAsReadAsync(Guid id);
		Task<BaseResponse<string>> MarkAllAsReadAsync(Guid userId);
	}
}
