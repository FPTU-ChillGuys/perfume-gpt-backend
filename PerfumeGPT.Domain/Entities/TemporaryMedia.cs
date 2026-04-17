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
		public static TemporaryMedia Create(TemporaryMediaPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.Url))
               throw DomainException.BadRequest("URL media tạm là bắt buộc.");

			if (string.IsNullOrWhiteSpace(payload.FileName))
             throw DomainException.BadRequest("Tên tệp media tạm là bắt buộc.");

			if (payload.FileSize <= 0)
              throw DomainException.BadRequest("Kích thước tệp media tạm phải lớn hơn 0.");

			return new TemporaryMedia
			{
				Url = payload.Url.Trim(),
				AltText = payload.AltText?.Trim(),
				DisplayOrder = payload.DisplayOrder,
				IsPrimary = payload.IsPrimary,
				PublicId = payload.PublicId?.Trim(),
				FileSize = payload.FileSize,
				MimeType = GetMimeType(payload.FileName),
				UploadedByUserId = payload.UploadedByUserId,
				TargetEntityType = payload.TargetEntityType,
				ExpiresAt = DateTime.UtcNow.Add(payload.ExpiresIn ?? TimeSpan.FromHours(24))
			};
		}

		// Business logic methods
		public void EnsureNotExpired()
		{
			if (IsExpired)
               throw DomainException.BadRequest("Media tạm đã hết hạn.");
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

		// Records
		public record TemporaryMediaPayload
		{
			public required string Url { get; init; }
			public required string FileName { get; init; }
			public required long FileSize { get; init; }
			public string? PublicId { get; init; }

			public string? AltText { get; init; }
			public int DisplayOrder { get; init; } = 0;
			public bool IsPrimary { get; init; } = false;

			public Guid? UploadedByUserId { get; init; }
			public required EntityType TargetEntityType { get; init; }
			public TimeSpan? ExpiresIn { get; init; }
		}
	}
}
