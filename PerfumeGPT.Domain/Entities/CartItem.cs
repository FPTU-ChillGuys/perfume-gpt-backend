using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class CartItem : BaseEntity<Guid>
    {
        public Guid CartId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }

        // Navigation
        public virtual Cart Cart { get; set; } = null!;
        public virtual ProductVariant ProductVariant { get; set; } = null!;
    }
}
