using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Hubs;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class SignalRService : ISignalRService
	{
		private readonly IHubContext<NotificationHub> _hubContext;
		private readonly IHubContext<PosHub, IPosClient> _posHubContext;

		public SignalRService(
			  IHubContext<NotificationHub> hubContext,
			  IHubContext<PosHub, IPosClient> posHubContext)
		{
			_hubContext = hubContext;
			_posHubContext = posHubContext;
		}

		public async Task NotifyNewOrderToStaff(Guid orderId, decimal totalAmount, string message)
		{
			await _hubContext.Clients.Groups("StaffGroup", "AdminGroup")
				 .SendAsync("ReceiveAdminNotification", new { orderId, totalAmount, message });
		}

		public async Task UpdateCustomerDisplayAsync(string sessionId, CartDisplayDto cartData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || cartData is null)
				return;

			await _posHubContext.Clients.Group(sessionId).UpdateCustomerDisplay(cartData);
		}

		public async Task NotifyPosPaymentCompletedAsync(string sessionId, PosPaymentCompletedDto paymentData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || paymentData is null)
				return;

			await _posHubContext.Clients.Group(sessionId).PaymentCompleted(paymentData);
		}
	}
}
