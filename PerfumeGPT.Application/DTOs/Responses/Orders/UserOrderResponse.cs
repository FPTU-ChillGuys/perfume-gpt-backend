using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record UserOrderResponse
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public OrderType Type { get; init; }
		public OrderStatus Status { get; init; }
		public bool IsReturnable { get; init; }
		public PaymentStatus PaymentStatus { get; init; }
		public decimal TotalAmount { get; init; }
		public decimal RequiredDepositAmount { get; init; }
		public decimal DepositAmount { get; init; }
		public decimal PaidAmount { get; init; }
		public decimal RemainingAmount { get; init; }
		public decimal SubTotal { get; init; }
		public decimal ShippingFee { get; init; }
		public string? VoucherCode { get; init; }
		public VoucherType? VoucherType { get; init; }
		public decimal VoucherDiscountTotal { get; init; }
		public DateTime? PaymentExpiresAt { get; init; }
		public DateTime? PaidAt { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }

		// Payment Info
		public List<PaymentInfoResponse>? PaymentTransactions { get; init; }

		// Shipping Info
		public ShippingInfoResponse? ShippingInfo { get; init; }

		// Recipient Info
		public RecipientInfoResponse? RecipientInfo { get; init; }

		// Order Details
		public required List<OrderDetailResponse> OrderDetails { get; init; }
	}
}
