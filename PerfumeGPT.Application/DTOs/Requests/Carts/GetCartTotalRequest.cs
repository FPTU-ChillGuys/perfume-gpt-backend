using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public class GetCartTotalRequest
	{
		public string? VoucherCode { get; set; }
		public List<Guid> ItemIds { get; set; } = [];
		public Guid? SavedAddressId { get; set; }
		public RecipientInformation? Recipient { get; set; }
	}
}
