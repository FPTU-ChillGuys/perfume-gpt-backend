namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public record GetCartTotalResponse
	{
		public decimal Subtotal { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { set; get; }
		public decimal TotalPrice { get; init; }
	}
}
