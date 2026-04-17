using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.Hubs
{
	//[Authorize]
	public class PosHub : Hub<IPosClient>
	{
		public async Task JoinPosSession(string sessionId)
		{
			if (string.IsNullOrWhiteSpace(sessionId))
				throw new HubException("Buộc phải cung cấp sessionId.");

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
				throw new HubException("Session và barcode không được để trống.");

			await Clients.OthersInGroup(sessionId).ReceiveBarcode(barcode);
		}

		public async Task SyncCartToCustomerDisplay(string sessionId, CartDisplayDto cartData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || cartData == null)
				throw new HubException("Session không được để trống và cartData không được null.");

			await Clients.OthersInGroup(sessionId).UpdateCustomerDisplay(cartData);
		}

		public async Task SyncOnlineOrderToCustomerDisplay(string sessionId, object orderData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || orderData == null)
				throw new HubException("Session không được để trống và orderData không được null.");

			await Clients.OthersInGroup(sessionId).ReceiveOnlineOrder(orderData);
		}

		public async Task NotifyPaymentSuccess(string sessionId, PosPaymentCompletedDto paymentData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || paymentData == null)
				throw new HubException("Session không được để trống và paymentData không được null.");

			await Clients.OthersInGroup(sessionId).PaymentCompleted(paymentData);
		}

		public async Task NotifyPaymentFailed(string sessionId, PosPaymentCompletedDto paymentData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || paymentData == null)
				throw new HubException("Session không được để trống và paymentData không được null.");

			await Clients.OthersInGroup(sessionId).PaymentFailed(paymentData);
		}

		public async Task NotifyPaymentLinkUpdated(string sessionId, PosPaymentLinkDto paymentData)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || paymentData == null)
				throw new HubException("Session không được để trống và paymentData không được null.");

			await Clients.OthersInGroup(sessionId).PaymentLinkUpdated(paymentData);
		}

		public async Task NotifyOrderDelivered(string sessionId, string orderCode)
		{
			if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(orderCode))
				throw new HubException("Session và orderCode không được để trống.");

			await Clients.OthersInGroup(sessionId).OrderDelivered(orderCode);
		}
	}
}
