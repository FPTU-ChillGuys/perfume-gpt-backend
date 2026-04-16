using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Payments;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IPosClient
	{
		Task ReceiveBarcode(string barcode);
		Task UpdateCustomerDisplay(CartDisplayDto cartData);
		Task PaymentCompleted(PosPaymentCompletedDto paymentData);
		Task PaymentFailed(PosPaymentCompletedDto paymentData);
		Task PaymentLinkUpdated(PosPaymentLinkDto paymentData);
		Task OrderDelivered(string orderCode);
		Task ReceiveOnlineOrder(object orderData);
	}
}
