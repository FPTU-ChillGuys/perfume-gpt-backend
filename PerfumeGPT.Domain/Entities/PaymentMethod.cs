using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class PaymentMethod : BaseEntity<int>
    {
        public string? Name { get; set; }

        // Navigation
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
    }
}
