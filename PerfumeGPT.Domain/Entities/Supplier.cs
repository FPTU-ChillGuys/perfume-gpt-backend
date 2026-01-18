using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
    public class Supplier : BaseEntity<int>
    {
        public string? Name { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }

        // Navigation
        public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];
    }
}
