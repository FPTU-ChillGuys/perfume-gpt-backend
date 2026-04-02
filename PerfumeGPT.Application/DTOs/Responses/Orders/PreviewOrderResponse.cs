using PerfumeGPT.Application.DTOs.Responses.Orders.OrderDetails;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record PreviewOrderResponse
	{
		public required List<OrderDetailListItems> Items { get; init; }
		public decimal SubTotal { get; init; }
		public decimal ShippingFee { get; init; }
		public decimal Discount { get; init; }
		public decimal Total { get; init; }
	}
}
