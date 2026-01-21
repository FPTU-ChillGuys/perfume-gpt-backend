using PerfumeGPT.Application.DTOs.Requests.OrderDetails;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class CreateInStoreOrderRequest
	{
		public Guid StaffId { get; set; }
		public Guid? VoucherId { get; set; }
		public bool IsPickupInStore { get; set; } = false;

		public List<CreateOrderDetailRequest> OrderDetails { get; set; } = new();
		public RecipientInformation? Recipient { get; set; }
		public PaymentInformation Payment { get; set; } = new PaymentInformation();
	}
}
