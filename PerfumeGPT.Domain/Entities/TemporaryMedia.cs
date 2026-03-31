using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class TemporaryMedia : BaseEntity<Guid>, IHasCreatedAt
	{
		private TemporaryMedia() { }

		public Guid? UploadedByUserId { get; private set; }
		public string Url { get; private set; } = null!;
		public string? AltText { get; private set; }
		public int DisplayOrder { get; private set; } = 0;
		public bool IsPrimary { get; private set; } = false;
		public string? PublicId { get; private set; }
		public long? FileSize { get; private set; }
		public string? MimeType { get; private set; }
		public EntityType? TargetEntityType { get; private set; }
		public DateTime ExpiresAt { get; private set; }
		public bool IsExpired => DateTime.UtcNow > ExpiresAt;

		// Navigation properties
		public virtual User? UploadedByUser { get; set; }

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static TemporaryMedia Create(
			string url,
			string fileName,
			long fileSize,
			Guid? uploadedByUserId,
			EntityType targetEntityType,
			int displayOrder,
			bool isPrimary = false,
			string? altText = null,
			string? publicId = null,
			TimeSpan? expiresIn = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw DomainException.BadRequest("Temporary media URL is required.");

			if (string.IsNullOrWhiteSpace(fileName))
				throw DomainException.BadRequest("Temporary media file name is required.");

			if (fileSize <= 0)
				throw DomainException.BadRequest("Temporary media file size must be greater than 0.");

			return new TemporaryMedia
			{
				Url = url.Trim(),
				AltText = altText,
				DisplayOrder = displayOrder,
				IsPrimary = isPrimary,
				PublicId = publicId,
				FileSize = fileSize,
				MimeType = GetMimeType(fileName),
				UploadedByUserId = uploadedByUserId,
				TargetEntityType = targetEntityType,
				ExpiresAt = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(24))
			};
		}

		// Business logic methods
		public void EnsureNotExpired()
		{
			if (IsExpired)
				throw DomainException.BadRequest("Temporary media has expired.");
		}

		private static string? GetMimeType(string fileName) =>
			Path.GetExtension(fileName).ToLowerInvariant() switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".webp" => "image/webp",
				".svg" => "image/svg+xml",
				".mp4" => "video/mp4",
				".mov" => "video/quicktime",
				".webm" => "video/webm",
				".m4v" => "video/x-m4v",
				_ => null
			};
	}
}
