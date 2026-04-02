using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record OrderListItem
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public Guid? CustomerId { get; init; }
		public string? CustomerName { get; init; }
		public Guid? StaffId { get; init; }
		public string? StaffName { get; init; }
		public OrderType Type { get; init; }
		public OrderStatus Status { get; init; }
		public PaymentStatus PaymentStatus { get; init; }
		public decimal TotalAmount { get; init; }
		public int ItemCount { get; init; }
		public bool IsReturnalbe { get; init; }
		public ShippingStatus? ShippingStatus { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
