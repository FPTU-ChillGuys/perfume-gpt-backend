using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

namespace PerfumeGPT.Domain.Entities
{
    public class Notification : BaseEntity<Guid>, IHasCreatedAt
    {
        public Guid UserId { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
        public Guid? StockId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? VoucherId { get; set; }
        public Guid? BatchId { get; set; }
        public bool IsRead { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual Stock? Stock { get; set; }
        public virtual Order? Order { get; set; }
        public virtual Voucher? Voucher { get; set; }
        public virtual Batch? Batch { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
    }
}
