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
		public Guid? OrderReturnRequestId { get; private set; }
		public Guid? SystemPageId { get; private set; }

		public Guid EntityId => EntityType switch
		{
			EntityType.Product => ProductId ?? Guid.Empty,
			EntityType.ProductVariant => ProductVariantId ?? Guid.Empty,
			EntityType.User => UserId ?? Guid.Empty,
			EntityType.Review => ReviewId ?? Guid.Empty,
			EntityType.OrderReturnRequest => OrderReturnRequestId ?? Guid.Empty,
			EntityType.SystemPage => SystemPageId ?? Guid.Empty,
			_ => Guid.Empty
		};

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ProductVariant? ProductVariant { get; set; }
		public virtual User? User { get; set; }
		public virtual Review? Review { get; set; }
		public virtual OrderReturnRequest? OrderReturnRequest { get; set; }
		public virtual SystemPage? SystemPage { get; set; }

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Media Create(EntityType entityType, Guid entityId, FileMetadata file, MediaDisplayInfo display)
		{
			ValidateUrl(file.Url);

			var media = new Media
			{
				Url = file.Url.Trim(),
				PublicId = file.PublicId,
				FileSize = file.FileSize,
				MimeType = file.MimeType,
				AltText = display.AltText,
				DisplayOrder = display.DisplayOrder,
				IsPrimary = display.IsPrimary,
				EntityType = entityType
			};

			media.SetEntityId(entityType, entityId);
			return media;
		}

		public static Media CreateBasic(EntityType entityType, Guid entityId, BasicMediaInfo info)
		{
			ValidateUrl(info.Url);

			var media = new Media
			{
				Url = info.Url.Trim(),
				AltText = info.AltText ?? "Profile picture",
				DisplayOrder = 0,
				IsPrimary = true,
				MimeType = info.MimeType,
				EntityType = entityType
			};

			media.SetEntityId(entityType, entityId);
			return media;
		}

		public void UpdateFile(FileMetadata file, string? altText = null)
		{
			ValidateUrl(file.Url);

			Url = file.Url.Trim();
			PublicId = file.PublicId;
			FileSize = file.FileSize;
			MimeType = file.MimeType;

			if (altText != null)
				AltText = altText;
		}

		// Business logic methods
		public void SetAsPrimary() => IsPrimary = true;
		public void UnsetPrimary() => IsPrimary = false;

		public void EnsureNotPrimary()
		{
			if (IsPrimary)
				throw DomainException.BadRequest(
					"Không thể xóa media này vì nó đang được đánh dấu là primary. Vui lòng đặt một media khác làm primary trước khi xóa."
				);
		}

		private void SetEntityId(EntityType entityType, Guid entityId)
		{
			switch (entityType)
			{
				case EntityType.Product: ProductId = entityId; break;
				case EntityType.ProductVariant: ProductVariantId = entityId; break;
				case EntityType.User: UserId = entityId; break;
				case EntityType.Review: ReviewId = entityId; break;
				case EntityType.OrderReturnRequest: OrderReturnRequestId = entityId; break;
				case EntityType.SystemPage: SystemPageId = entityId; break;
			}
		}

		private static void ValidateUrl(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw DomainException.BadRequest("URL là bắt buộc và không được để trống.");
		}

		// Records
		public record FileMetadata
		{
			public required string Url { get; init; }
			public string? PublicId { get; init; }
			public long? FileSize { get; init; }
			public string? MimeType { get; init; }
		}

		public record MediaDisplayInfo
		{
			public string? AltText { get; init; }
			public int DisplayOrder { get; init; } = 0;
			public bool IsPrimary { get; init; } = false;
		}

		public record BasicMediaInfo
		{
			public required string Url { get; init; }
			public string? AltText { get; init; }
			public string? MimeType { get; init; }
		}
	}
}
