using PerfumeGPT.Domain.Commons;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportDetail : BaseEntity<Guid>
	{
		public Guid ImportId { get; set; }
		public Guid ProductVariantId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		public int RejectQuantity { get; set; } = 0;
		public string? Note { get; set; }

		// Navigation
		public virtual ImportTicket ImportTicket { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ICollection<Batch> Batches { get; set; } = [];
	}
}
