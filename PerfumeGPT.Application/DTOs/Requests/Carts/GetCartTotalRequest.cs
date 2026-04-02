using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public record GetCartTotalRequest
	{
		public string? VoucherCode { get; init; }
		public List<Guid> ItemIds { get; init; } = [];
		public Guid? SavedAddressId { get; init; }
		public RecipientInformation? Recipient { get; init; }
	}
}
