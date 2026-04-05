using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.OrderReturnRequests
{
	public record OrderReturnRequestResponse
	{
		public Guid Id { get; init; }
		public Guid OrderId { get; init; }
		public required string OrderCode { get; init; }
		public Guid CustomerId { get; init; }
		public string? CustomerEmail { get; init; }
		public Guid? ProcessedById { get; init; }
		public string? ProcessedByName { get; init; }
		public Guid? InspectedById { get; init; }
		public string? InspectedByName { get; init; }

		public required string Reason { get; init; }
		public string? CustomerNote { get; init; }
		public string? StaffNote { get; init; }
		public string? InspectionNote { get; init; }
		public ReturnRequestStatus Status { get; init; }

		public decimal RequestedRefundAmount { get; init; }
		public decimal? ApprovedRefundAmount { get; init; }
		public bool IsRefunded { get; init; }
		public bool IsRefundOnly { get; init; }
		public string? VnpTransactionNo { get; init; }
		public bool IsRestocked { get; init; }
		public ReturnShippingInfoResponse? ReturnShippingInfo { get; init; }
		public List<OrderReturnRequestDetailResponse>? ReturnDetails { get; init; }

		public List<MediaResponse>? ProofImages { get; init; }

		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}

	public record ReturnShippingInfoResponse
	{
		public Guid Id { get; init; }
		public CarrierName CarrierName { get; init; }
		public string? TrackingNumber { get; init; }
		public ShippingType Type { get; init; }
		public decimal ShippingFee { get; init; }
		public ShippingStatus Status { get; init; }
		public DateTime? EstimatedDeliveryDate { get; init; }
		public DateTime? ShippedDate { get; init; }
	}

	public record OrderReturnRequestDetailResponse
	{
		public Guid Id { get; init; }
		public Guid OrderDetailId { get; init; }
		public Guid VariantId { get; init; }
		public int RequestedQuantity { get; init; }
		public decimal UnitPrice { get; init; }
		public decimal RefundableAmount { get; init; }
	}
}
