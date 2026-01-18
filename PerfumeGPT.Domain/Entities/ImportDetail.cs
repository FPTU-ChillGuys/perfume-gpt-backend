using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class ImportDetail : BaseEntity<Guid>
    {
        public Guid ImportId { get; set; }
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation
        public virtual ImportTicket ImportTicket { get; set; } = null!;
        public virtual ProductVariant ProductVariant { get; set; } = null!;
        public virtual ICollection<Batch> Batches { get; set; } = [];
    }
}
