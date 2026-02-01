using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class TemporaryMedia : BaseEntity<Guid>, IHasCreatedAt
	{
		public string Url { get; set; } = null!;
		public string? AltText { get; set; }
		public int DisplayOrder { get; set; } = 0;
		public bool IsPrimary { get; set; } = false; // For Product/Variant images
		public string? PublicId { get; set; } // For cloud storage (e.g., Supabase)
		public long? FileSize { get; set; } // In bytes
		public string? MimeType { get; set; } // e.g., image/jpeg, image/png
		public EntityType? TargetEntityType { get; set; }

		// Track who uploaded it
		public Guid? UploadedByUserId { get; set; }

		// Auto-expire after 24 hours
		public DateTime ExpiresAt { get; set; }

		// Navigation
		public virtual User? UploadedByUser { get; set; }

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Helper method
		public bool IsExpired => DateTime.UtcNow > ExpiresAt;
	}
}
