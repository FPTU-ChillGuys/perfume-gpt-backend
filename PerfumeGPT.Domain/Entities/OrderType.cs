using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class OrderType : BaseEntity<int>
    {
        public string? Name { get; set; }

        // Navigation
        public virtual ICollection<Order> Orders { get; set; } = [];
    }
}
