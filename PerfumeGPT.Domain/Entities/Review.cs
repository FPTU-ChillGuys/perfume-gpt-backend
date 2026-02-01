using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Review : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public Guid UserId { get; set; }
		public Guid OrderDetailId { get; set; }
		public int Rating { get; set; } // 1-5 stars
		public string Comment { get; set; } = string.Empty;
		public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

		// Moderation
		public Guid? ModeratedByStaffId { get; set; }
		public DateTime? ModeratedAt { get; set; }
		public string? ModerationReason { get; set; }

		// Navigation
		public virtual User User { get; set; } = null!;
		public virtual OrderDetail OrderDetail { get; set; } = null!;
		public virtual User? ModeratedByStaff { get; set; }
		public virtual ICollection<Media> ReviewImages { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
