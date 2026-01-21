using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class ShippingInfo : BaseEntity<Guid>
	{
		public Guid OrderId { get; set; }
		public CarrierName CarrierName { get; set; }
		public string? TrackingNumber { get; set; }
		public decimal ShippingFee { get; set; }
		public ShippingStatus Status { get; set; }

		// Navigation
		public virtual Order Order { get; set; } = null!;
	}
}
