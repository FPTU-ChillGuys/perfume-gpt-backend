using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Stock : BaseEntity<Guid>
    {
        public Guid VariantId { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockThreshold { get; set; }

        // Navigation
        public virtual ProductVariant ProductVariant { get; set; } = null!;
        public virtual ICollection<Notification> Notifications { get; set; } = [];
    }
}
