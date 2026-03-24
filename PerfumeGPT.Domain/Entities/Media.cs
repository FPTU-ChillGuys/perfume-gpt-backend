using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Media : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		private Media() { }

		public string Url { get; private set; } = null!;
		public string? AltText { get; private set; }
		public int DisplayOrder { get; private set; } = 0;
		public bool IsPrimary { get; private set; } = false;
		public string? PublicId { get; private set; }
		public long? FileSize { get; private set; }
		public string? MimeType { get; private set; }

		public EntityType EntityType { get; private set; }
		public Guid? ProductId { get; private set; }
		public Guid? ProductVariantId { get; private set; }
		public Guid? UserId { get; private set; }
		public Guid? ReviewId { get; private set; }

		public Guid EntityId => EntityType switch
		{
			EntityType.Product => ProductId ?? Guid.Empty,
			EntityType.ProductVariant => ProductVariantId ?? Guid.Empty,
			EntityType.User => UserId ?? Guid.Empty,
			EntityType.Review => ReviewId ?? Guid.Empty,
			_ => Guid.Empty
		};

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ProductVariant? ProductVariant { get; set; }
		public virtual User? User { get; set; }
		public virtual Review? Review { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Media CreateForEntity(
		EntityType entityType,
		Guid entityId,
		string url,
		string? altText,
		int displayOrder,
		bool isPrimary,
		string? publicId,
		long? fileSize,
		string? mimeType)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw DomainException.BadRequest("Media URL is required.");

			var media = new Media
			{
				Url = url.Trim(),
				AltText = altText,
				DisplayOrder = displayOrder,
				IsPrimary = isPrimary,
				PublicId = publicId,
				FileSize = fileSize,
				MimeType = mimeType,
				EntityType = entityType
			};

			media.SetEntityId(entityType, entityId);
			return media;
		}

		public static Media CreateFromUrl(
		EntityType entityType,
		Guid entityId,
		string url,
		string? altText = null,
		string? mimeType = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw DomainException.BadRequest("Media URL is required.");

			var media = new Media
			{
				Url = url.Trim(),
				AltText = altText ?? "Profile picture",
				DisplayOrder = 0,
				IsPrimary = true,
				MimeType = mimeType,
				EntityType = entityType
			};

			media.SetEntityId(entityType, entityId);
			return media;
		}

		// Domain methods
		public void SetAsPrimary() => IsPrimary = true;
		public void UnsetPrimary() => IsPrimary = false;

		public void UpdateUrl(string url, string? publicId, long? fileSize, string? mimeType, string? altText)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw DomainException.BadRequest("Media URL is required.");

			Url = url.Trim();
			PublicId = publicId;
			FileSize = fileSize;
			MimeType = mimeType;
			if (altText != null) AltText = altText;
		}

		public void EnsureNotPrimary()
		{
			if (IsPrimary)
				throw DomainException.BadRequest(
					"Cannot delete primary media. Please set another media as primary before deleting.");
		}

		private void SetEntityId(EntityType entityType, Guid entityId)
		{
			switch (entityType)
			{
				case EntityType.Product: ProductId = entityId; break;
				case EntityType.ProductVariant: ProductVariantId = entityId; break;
				case EntityType.User: UserId = entityId; break;
				case EntityType.Review: ReviewId = entityId; break;
			}
		}
	}
}
