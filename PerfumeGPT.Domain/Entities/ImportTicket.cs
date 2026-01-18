using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class ImportTicket : BaseEntity<Guid>
    {
        public Guid CreatedById { get; set; }
        public int SupplierId { get; set; }
        public DateTime ImportDate { get; set; }
        public decimal TotalCost { get; set; }
        public string? Status { get; set; }

        // Navigation
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual Supplier Supplier { get; set; } = null!;
        public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];
    }
}
