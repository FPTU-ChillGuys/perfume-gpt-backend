using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.Hubs
{
	[Authorize]
	public class PosHub : Hub<IPosClient>
	{
		public async Task JoinPosSession(string sessionId)
		{
			if (string.IsNullOrWhiteSpace(sessionId))
				throw new HubException("SessionId is required.");

			await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
		}

		public async Task LeavePosSession(string sessionId)
		{
			if (!string.IsNullOrWhiteSpace(sessionId))
			{
				await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
			}
		}

		public async Task SendBarcodeFromMobile(string sessionId, string barcode)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(barcode))
				throw new HubException("Invalid session or barcode.");

			await Clients.OthersInGroup(sessionId).ReceiveBarcode(barcode);
		}

		public async Task SyncCartToCustomerDisplay(string sessionId, CartDisplayDto cartData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || cartData == null)
				throw new HubException("Invalid session or cart data.");

			await Clients.OthersInGroup(sessionId).UpdateCustomerDisplay(cartData);
		}

		public async Task NotifyPaymentSuccess(string sessionId, PosPaymentCompletedDto paymentData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || paymentData == null)
				throw new HubException("Invalid session or payment data.");

			await Clients.OthersInGroup(sessionId).PaymentCompleted(paymentData);
		}
	}
}
