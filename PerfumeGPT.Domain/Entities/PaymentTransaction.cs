using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class PaymentTransaction : BaseEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public int MethodId { get; set; }
        public string? TransactionStatus { get; set; }
        public decimal Amount { get; set; }
        public DateTime ProcessedAt { get; set; }

        // Navigation
        public virtual Order? Order { get; set; }
        public virtual PaymentMethod? PaymentMethod { get; set; }
        public virtual Receipt? Receipt { get; set; }
    }
}
