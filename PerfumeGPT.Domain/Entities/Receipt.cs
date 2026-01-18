using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Receipt : BaseEntity<Guid>
    {
        public Guid TransactionId { get; set; }
        public string ReceiptNumber { get; set; } = null!;
        public DateTime IssuedAt { get; set; }

        // Navigation
        public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;
    }
}
