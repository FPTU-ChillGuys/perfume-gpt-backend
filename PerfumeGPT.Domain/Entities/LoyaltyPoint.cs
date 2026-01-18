using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
    public class LoyaltyPoint : BaseEntity<Guid>, IFullAuditable
    {
        public Guid UserId { get; set; }
        public int PointBalance { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;

        // Audit
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}