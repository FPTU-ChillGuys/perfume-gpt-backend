using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Hubs;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class SignalRService : ISignalRService
	{
		private readonly IHubContext<NotificationHub> _hubContext;

		public SignalRService(IHubContext<NotificationHub> hubContext)
		{
			_hubContext = hubContext;
		}

		public async Task NotifyNewOrderToStaff(string orderId, decimal totalAmount)
		{
			await _hubContext.Clients.Group("StaffGroup")
				.SendAsync("ReceiveAdminNotification", new { orderId, totalAmount });
		}

		public async Task NotifyProductCreated(Guid id)
		{
			await _hubContext.Clients.Group("StaffGroup")
				.SendAsync("ProductCreated", new { id });
		}
	}
}
