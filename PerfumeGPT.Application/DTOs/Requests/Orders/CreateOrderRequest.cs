using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class CreateOrderRequest
	{
		public Guid CustomerId { get; set; }
		public Guid StaffId { get; set; }
		public string? ExternalShopeeId { get; set; }
		public Guid? VoucherId { get; set; }
		public bool IsPickupInStore { get; set; } = false;

		public RecipientInformation? Recipient { get; set; }
		public PaymentInformation Payment { get; set; } = new PaymentInformation();
	}

	public class RecipientInformation
	{
		public string FullName { get; set; } = null!;
		public string Phone { get; set; } = null!;
		public int DistrictId { get; set; }
		public string WardCode { get; set; } = null!;
		public string FullAddress { get; set; } = null!;
	}

	public class PaymentInformation
	{
		public PaymentMethod Method { get; set; }
	}
}
