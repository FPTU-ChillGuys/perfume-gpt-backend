using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Shippings
{
	public class ShippingInfoListItem
	{
		public Guid Id { get; set; }
		public Guid OrderId { get; set; }
		public CarrierName CarrierName { get; set; }
		public string? TrackingNumber { get; set; }
		public decimal ShippingFee { get; set; }
		public ShippingStatus Status { get; set; }
		public int? LeadTime { get; set; }
		public DateTime? ShippedDate { get; set; }
	}
}
