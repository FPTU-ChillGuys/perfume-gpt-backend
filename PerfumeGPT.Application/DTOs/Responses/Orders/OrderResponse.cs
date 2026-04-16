using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record OrderResponse
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public Guid? CustomerId { get; init; }
		public string? CustomerName { get; init; }
		public string? CustomerEmail { get; init; }
		public string? CustomerPhoneNumber { get; init; }
		public Guid? StaffId { get; init; }
		public string? StaffName { get; init; }
		public OrderType Type { get; init; }
		public OrderStatus Status { get; init; }
		public PaymentStatus PaymentStatus { get; init; }
		public decimal TotalAmount { get; init; }
		public decimal SubTotal { get; init; }
		public decimal ShippingFee { get; init; }
		public Guid? VoucherId { get; init; }
		public string? VoucherCode { get; init; }
		public VoucherType? VoucherType { get; init; }
		public decimal VoucherDiscountTotal { get; init; }
		public DateTime? PaymentExpiresAt { get; init; }
		public DateTime? PaidAt { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }

		// payment Info
		public List<PaymentInfoResponse>? PaymentTransactions { set; get; }

		// Shipping Info
		public ShippingInfoResponse? ShippingInfo { get; init; }

		// Recipient Info
		public RecipientInfoResponse? RecipientInfo { get; init; }

		// Order Details
		public required List<OrderDetailResponse> OrderDetails { get; init; }
	}

	public record PaymentInfoResponse
	{
		public Guid Id { get; init; }
		public TransactionType TransactionType { get; init; }
		public TransactionStatus Status { get; init; }
		public PaymentMethod PaymentMethod { get; init; }
		public string? FailureReason { get; init; }
		public decimal TotalAmount { get; init; }
	}

	public record ShippingInfoResponse
	{
		public Guid Id { get; init; }
		public CarrierName CarrierName { get; init; }
		public string? TrackingNumber { get; init; }
		public decimal ShippingFee { get; init; }
		public ShippingStatus Status { get; init; }
		public DateTime? EstimatedDeliveryDate { get; init; }
		public DateTime? ShippedDate { get; init; }
	}

	public record RecipientInfoResponse
	{
		public Guid Id { get; init; }
		public string? RecipientName { get; init; }
		public string? RecipientPhoneNumber { get; init; }
		public required string DistrictName { get; init; }
		public required string WardName { get; init; }
		public required string ProvinceName { get; init; }
		public required string FullAddress { get; init; }
	}

	public record OrderDetailResponse
	{
		public Guid Id { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public string? ImageUrl { get; init; }
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal CampaignDiscount { get; init; }
		public decimal CampaignPrice { get; init; }
		public decimal VoucherDiscount { get; init; }
		public decimal ItemTotal { get; init; }
		public decimal RefunablePrice { get; init; }
		public decimal Total { get; init; }
		public required List<ReservedBatchResponse> ReservedBatches { get; init; }
	}

	public record ReservedBatchResponse
	{
		public Guid BatchId { get; init; }
		public required string BatchCode { get; init; }
		public int ReservedQuantity { get; init; }
		public DateTime ExpiryDate { get; init; }
	}
}
