using PerfumeGPT.Application.DTOs.Requests.Orders.OrderDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record CreateInStoreOrderRequest
	{
		public string? VoucherCode { get; init; }
		public bool IsPickupInStore { get; init; } = false;

		public required List<CreateOrderDetailRequest> OrderDetails { get; init; }
		public RecipientInformation? Recipient { get; init; }
		public required PaymentInformation Payment { get; init; }
	}
}
