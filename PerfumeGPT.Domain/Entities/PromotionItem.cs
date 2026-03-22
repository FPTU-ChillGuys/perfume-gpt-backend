using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class PromotionItem : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public Guid CampaignId { get; set; }
		public Guid ProductVariantId { get; set; }
		public Guid? BatchId { get; set; }
		public string Name { get; set; } = string.Empty;
		public PromotionType ItemType { get; set; } // Clearance, NewArrival, Regular, etc.
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public bool AutoStopWhenBatchEmpty { get; set; }
		public int? MaxUsage { get; set; }
		public int CurrentUsage { get; set; }

		// Navigation properties
		public virtual Campaign Campaign { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Batch? Batch { get; set; }

		// IHasTimestamps  implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
