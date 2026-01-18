using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class ShippingInfo : BaseEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public string? CarrierName { get; set; }
        public string? TrackingNumber { get; set; }
        public decimal ShippingFee { get; set; }
        public string? Status { get; set; }

        // Navigation
        public virtual Order Order { get; set; } = null!;
    }
}
