using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class UserVoucher : BaseEntity<Guid>
    {
        public Guid UserId { get; set; }
        public Guid VoucherId { get; set; }
        public bool IsUsed { get; set; }

        // Navigation
        public virtual User User { get; set; } = null!;
        public virtual Voucher Voucher { get; set; } = null!;
    }
}
