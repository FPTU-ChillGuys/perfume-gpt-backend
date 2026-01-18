using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class FragranceFamily : BaseEntity<int>
    {
        public string? Name { get; set; }

        // Navigation
        public virtual ICollection<Product> Products { get; set; } = [];
    }
}
