using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record CartDisplayDto
	{
		public required List<PosOrderDetailListItem> Items { get; init; }
		public decimal SubTotal { get; init; }
		public decimal Discount { get; init; }
		public decimal TotalPrice { get; init; }
		public string? PaymentUrl { get; init; }
	}
}
