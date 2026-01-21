using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportTicket : BaseEntity<Guid>, IHasCreatedAt, ISoftDelete
	{
		public Guid CreatedById { get; set; }
		public int SupplierId { get; set; }
		public DateTime ImportDate { get; set; }
		public decimal TotalCost { get; set; }
		public ImportStatus Status { get; set; }

		// Navigation
		public virtual User CreatedByUser { get; set; } = null!;
		public virtual Supplier Supplier { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];

		// Audit
		public DateTime CreatedAt { get; set; }

		// Soft Delete
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
