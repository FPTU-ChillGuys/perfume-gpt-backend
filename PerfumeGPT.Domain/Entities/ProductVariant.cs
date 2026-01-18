using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class ProductVariant : BaseEntity<Guid>
    {
        public Guid ProductId { get; set; }
        public string Sku { get; set; } = null!;
        public int VolumeMl { get; set; }
        public int ConcentrationId { get; set; }
        public string? Type { get; set; }
        public decimal BasePrice { get; set; }
        public string? Status { get; set; }

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual Concentration Concentration { get; set; } = null!;
        public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];
        public virtual ICollection<Batch> Batches { get; set; } = [];
        public virtual Stock Stock { get; set; } = null!;
        public virtual ICollection<CartItem> CartItems { get; set; } = [];
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = [];
    }
}
