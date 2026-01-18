using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class RecipientInfo : BaseEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Navigation
        public virtual Order Order { get; set; } = null!;
    }
}
