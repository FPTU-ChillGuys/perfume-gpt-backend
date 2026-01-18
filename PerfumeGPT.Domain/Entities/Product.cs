using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Product : BaseEntity<Guid>
    {
        public string? Name { get; set; }
        public int BrandId { get; set; }
        public int CategoryId { get; set; }
        public int FamilyId { get; set; }
        public string? Description { get; set; }
        public string? TopNotes { get; set; }
        public string? MiddleNotes { get; set; }
        public string? BaseNotes { get; set; }

        // Navigation
        public virtual Brand Brand { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual FragranceFamily FragranceFamily { get; set; } = null!;
        public virtual ICollection<ProductVariant> Variants { get; set; } = [];
    }
}
