using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Campaign : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public CampaignType Type { get; set; } // Flash Sale, Clearance, Seasonal, etc.
		public CampaignStatus Status { get; set; } // Upcoming, Active, Paused, Completed, Cancelled

		// Navigation properties
		public virtual ICollection<PromotionItem> Items { get; set; } = [];
		public virtual ICollection<Voucher> Vouchers { get; set; } = [];

		// IHasTimestamps  implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
	}
}
