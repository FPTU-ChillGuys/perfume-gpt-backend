using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Shippings
{
	public record ShippingInfoListItem
	{
		public Guid Id { get; init; }
		public Guid OrderId { get; init; }
		public CarrierName CarrierName { get; init; }
		public string? TrackingNumber { get; init; }
		public decimal ShippingFee { get; init; }
		public ShippingStatus Status { get; init; }
		public int? LeadTime { get; init; }
		public DateTime? ShippedDate { get; init; }
	}
}
