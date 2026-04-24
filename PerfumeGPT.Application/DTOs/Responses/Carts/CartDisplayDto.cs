using PerfumeGPT.Application.DTOs.Responses.CartItems;

namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record CartDisplayDto
	{
		public required List<PosOrderDetailListItem> Items { get; init; }
		public decimal SubTotal { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { get; init; }
		public decimal TotalPrice { get; init; }

		// BỔ SUNG 2 TRƯỜNG NÀY:
		public decimal RequiredDepositAmount { get; init; }
		public bool IsDepositRequired => RequiredDepositAmount > 0;

		public string? PaymentUrl { get; init; }
		public string? Message { get; init; }
		public string? VoucherCode { get; init; }
	}
}
