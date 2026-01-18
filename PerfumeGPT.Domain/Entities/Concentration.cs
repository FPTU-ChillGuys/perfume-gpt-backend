using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Concentration : BaseEntity<int>
    {
        public string? Name { get; set; }

        // Navigation
        public virtual ICollection<ProductVariant> Variants { get; set; } = [];
    }
}
