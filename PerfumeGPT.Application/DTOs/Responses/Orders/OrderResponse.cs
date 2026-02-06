using PerfumeGPT.Application.DTOs.Responses.OrderDetails;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class OrderResponse
	{
		public Guid Id { get; set; }
		public Guid? CustomerId { get; set; }
		public string? CustomerName { get; set; }
		public string? CustomerEmail { get; set; }
		public Guid? StaffId { get; set; }
		public string? StaffName { get; set; }
		public OrderType Type { get; set; }
		public OrderStatus Status { get; set; }
		public PaymentStatus PaymentStatus { get; set; }
		public decimal TotalAmount { get; set; }
		public Guid? VoucherId { get; set; }
		public string? VoucherCode { get; set; }
		public DateTime? PaymentExpiresAt { get; set; }
		public DateTime? PaidAt { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Shipping Info
		public ShippingInfoResponse? ShippingInfo { get; set; }

		// Recipient Info
		public RecipientInfoResponse? RecipientInfo { get; set; }

		// Order Details
		public List<OrderDetailResponse> OrderDetails { get; set; } = [];
	}

	public class ShippingInfoResponse
	{
		public Guid Id { get; set; }
		public CarrierName CarrierName { get; set; }
		public string? TrackingNumber { get; set; }
		public decimal ShippingFee { get; set; }
		public ShippingStatus Status { get; set; }
		public int? LeadTime { get; set; }
	}

	public class RecipientInfoResponse
	{
		public Guid Id { get; set; }
		public string? FullName { get; set; }
		public string? Phone { get; set; }
		public string DistrictName { get; set; } = null!;
		public string WardName { get; set; } = null!;
		public string ProvinceName { get; set; } = null!;
		public string FullAddress { get; set; } = null!;
	}

	public class OrderDetailResponse
	{
		public Guid Id { get; set; }
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public string? ImageUrl { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal Total { get; set; }
	}
}
