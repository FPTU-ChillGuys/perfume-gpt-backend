using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class UserOrderResponse
	{
		public Guid Id { get; set; }
		public OrderType Type { get; set; }
		public OrderStatus Status { get; set; }
		public PaymentStatus PaymentStatus { get; set; }
		public decimal TotalAmount { get; set; }
		public string? VoucherCode { get; set; }
		public DateTime? PaymentExpiresAt { get; set; }
		public DateTime? PaidAt { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Payment Info
		public List<PaymentInfoResponse>? PaymentTransactions { get; set; }

		// Shipping Info
		public ShippingInfoResponse? ShippingInfo { get; set; }

		// Recipient Info
		public RecipientInfoResponse? RecipientInfo { get; set; }

		// Order Details
		public List<OrderDetailResponse> OrderDetails { get; set; } = [];
	}
}
