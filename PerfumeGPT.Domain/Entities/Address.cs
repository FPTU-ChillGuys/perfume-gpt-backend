using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
    public class Address : BaseEntity<Guid>, IHasTimestamps
    {
        public Guid UserId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = null!;

        // Address details
        public string Street { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int WardId { get; set; }
        public int DistrictId { get; set; }
        public int ProvinceId { get; set; }

        // Is default address
        public bool IsDefault { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
    }
}