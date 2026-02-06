namespace PerfumeGPT.Application.DTOs.Responses.Carts
{
	public class GetCartTotalResponse
	{
		public decimal Subtotal { get; set; }
		public decimal ShippingFee { get; set; }
		public decimal Discount => Subtotal + ShippingFee - TotalPrice;
		public decimal TotalPrice { get; set; }
	}
}
