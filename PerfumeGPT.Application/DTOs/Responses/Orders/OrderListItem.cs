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
		public DateTime? PaymentExpiresAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
		public required List<OrderDetailListItem> OrderDetails { get; init; }
		public List<PaymentInfoResponse>? PaymentTransactions { set; get; }
	}

	public record OrderDetailListItem
	{
		public Guid Id { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public string? ImageUrl { get; init; }
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal RefunablePrice { get; init; }
		public decimal Total { get; init; }
	}
}
