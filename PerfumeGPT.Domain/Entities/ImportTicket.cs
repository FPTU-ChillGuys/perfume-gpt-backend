using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportTicket : BaseEntity<Guid>, IUpdateAuditable, IHasCreatedAt, ISoftDelete
	{
		public Guid CreatedById { get; set; }
		public Guid? VerifiedById { get; set; }
		public int SupplierId { get; set; }
		public DateTime ImportDate { get; set; }
		public decimal TotalCost { get; set; }
		public ImportStatus Status { get; set; }

		// Navigation
		public virtual User CreatedByUser { get; set; } = null!;
		public virtual User? VerifiedByUser { get; set; }
		public virtual Supplier Supplier { get; set; } = null!;
		public virtual ICollection<ImportDetail> ImportDetails { get; set; } = [];

		// Audit
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		// Soft Delete
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
