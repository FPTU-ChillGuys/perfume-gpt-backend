using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class OrderListItem
	{
		public Guid Id { get; set; }
		public Guid? CustomerId { get; set; }
		public string? CustomerName { get; set; }
		public Guid? StaffId { get; set; }
		public string? StaffName { get; set; }
		public OrderType Type { get; set; }
		public OrderStatus Status { get; set; }
		public PaymentStatus PaymentStatus { get; set; }
		public decimal TotalAmount { get; set; }
		public int ItemCount { get; set; }
		public ShippingStatus? ShippingStatus { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
