using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
    public class CustomerProfile : BaseEntity<Guid>, IHasTimestamps
    {
        public Guid UserId { get; set; }
        public string? ScentPreference { get; set; }
        public decimal? MinBudget { get; set; }
        public decimal? MaxBudget { get; set; }
        public string? PreferredStyle { get; set; }
        public string? FavoriteNotes { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;

        // Audit
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
