using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class CreateOrderRequest
	{
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
		public string DistrictName { get; set; } = null!;
		public string WardCode { get; set; } = null!;
		public string WardName { get; set; } = null!;
		public int ProvinceId { get; set; }
		public string ProvinceName { get; set; } = null!;
		public string FullAddress { get; set; } = null!;
	}

	public class PaymentInformation
	{
		public PaymentMethod Method { get; set; }
	}
}
