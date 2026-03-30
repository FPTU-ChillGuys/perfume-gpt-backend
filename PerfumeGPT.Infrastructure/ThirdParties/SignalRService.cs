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

		public async Task NotifyNewOrderToStaff(Guid orderId, decimal totalAmount, string message)
		{
			await _hubContext.Clients.Groups("StaffGroup", "AdminGroup")
				 .SendAsync("ReceiveAdminNotification", new { orderId, totalAmount, message });
		}
	}
}
