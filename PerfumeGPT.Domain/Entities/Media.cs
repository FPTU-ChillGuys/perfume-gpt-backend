using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class Media : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		public string Url { get; set; } = null!;
		public string? AltText { get; set; }
		public EntityType EntityType { get; set; }

		// Separate foreign keys for proper referential integrity
		public Guid? ProductId { get; set; }
		public Guid? ProductVariantId { get; set; }
		public Guid? ReviewId { get; set; }

		// Computed property for backward compatibility
		public Guid EntityId => EntityType switch
		{
			EntityType.Product => ProductId ?? Guid.Empty,
			EntityType.ProductVariant => ProductVariantId ?? Guid.Empty,
			EntityType.Review => ReviewId ?? Guid.Empty,
			_ => Guid.Empty
		};

		public int DisplayOrder { get; set; } = 0;
		public bool IsPrimary { get; set; } = false;
		public string? PublicId { get; set; } // For cloud storage (e.g., Cloudinary)
		public long? FileSize { get; set; } // In bytes
		public string? MimeType { get; set; } // e.g., image/jpeg, image/png

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ProductVariant? ProductVariant { get; set; }
		public virtual Review? Review { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
