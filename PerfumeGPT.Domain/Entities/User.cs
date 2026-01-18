using Microsoft.AspNetCore.Identity;
using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
    public class User : IdentityUser<Guid>, IHasTimestamps, ISoftDelete
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Profile
        public string? ProfilePictureUrl { get; set; }

        // Navigations
        public virtual CustomerProfile? CustomerProfile { get; set; }
        public virtual LoyaltyPoint? LoyaltyPoint { get; set; }
        public virtual ICollection<Address> Addresses { get; set; } = [];
        public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
        public virtual ICollection<Notification> Notifications { get; set; } = [];
        public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
        public virtual Cart? Cart { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = [];

        // Audit
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Soft Delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
