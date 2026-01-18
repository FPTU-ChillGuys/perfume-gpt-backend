using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Cart : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual ICollection<CartItem> Items { get; set; } = [];
    }
}
