using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Banner : BaseEntity<Guid>, IHasTimestamps
	{
		protected Banner() { }

		// 1. Thông tin hiển thị
		public string Title { get; private set; } = null!;
		public string ImageUrl { get; private set; } = null!;
		public string? ImagePublicId { get; private set; } // BỔ SUNG: Để xóa file rác trên Cloud

		public string? MobileImageUrl { get; private set; }
		public string? MobileImagePublicId { get; private set; } //  BỔ SUNG

		public string? AltText { get; private set; }

		// 2. Vị trí và Thứ tự
		public BannerPosition Position { get; private set; }
		public int DisplayOrder { get; private set; }

		// 3. Thời gian hiển thị
		public bool IsActive { get; private set; }
		public DateTime? StartDate { get; private set; }
		public DateTime? EndDate { get; private set; }

		// 4. Cấu hình Điều hướng (Polymorphic Link - Mối quan hệ ảo)
		public BannerLinkType LinkType { get; private set; }
		public string? LinkTarget { get; private set; }

		// IHasTimestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// --- Factory Method ---
		public static Banner Create(BannerCreationPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.Title)) throw DomainException.BadRequest("Tiêu đề là bắt buộc.");
			if (string.IsNullOrWhiteSpace(payload.ImageUrl)) throw DomainException.BadRequest("URL hình ảnh là bắt buộc.");

			ValidateLink(payload.LinkType, payload.LinkTarget);

			return new Banner
			{
				Title = payload.Title.Trim(),
				AltText = payload.AltText?.Trim(),
				ImageUrl = payload.ImageUrl.Trim(),
				ImagePublicId = payload.ImagePublicId?.Trim(),
				MobileImageUrl = payload.MobileImageUrl?.Trim(),
				MobileImagePublicId = payload.MobileImagePublicId?.Trim(),
				Position = payload.Position,
				LinkType = payload.LinkType,
				LinkTarget = payload.LinkTarget?.Trim(),
				IsActive = true, // Mặc định là Active
				DisplayOrder = 0 // Mặc định xếp đầu
			};
		}

		// --- Business Logic ---

		// Cập nhật nội dung hiển thị (Hình ảnh, Text)
		public void UpdateContent(string title, string imageUrl, string? imagePublicId, string? mobileImageUrl, string? mobileImagePublicId, string? altText)
		{
			if (string.IsNullOrWhiteSpace(title)) throw DomainException.BadRequest("Tiêu đề là bắt buộc.");
			if (string.IsNullOrWhiteSpace(imageUrl)) throw DomainException.BadRequest("URL hình ảnh là bắt buộc.");

			Title = title.Trim();
			ImageUrl = imageUrl.Trim();
			ImagePublicId = imagePublicId?.Trim();
			MobileImageUrl = mobileImageUrl?.Trim();
			MobileImagePublicId = mobileImagePublicId?.Trim();
			AltText = altText?.Trim();
		}

		// Cập nhật đích đến (Mối quan hệ)
		public void UpdateLink(BannerLinkType linkType, string? linkTarget)
		{
			ValidateLink(linkType, linkTarget);
			LinkType = linkType;
			LinkTarget = linkTarget?.Trim();
		}

		public void UpdateSchedule(DateTime? startDate, DateTime? endDate)
		{
			if (startDate.HasValue && endDate.HasValue && startDate >= endDate)
				throw DomainException.BadRequest("Ngày kết thúc phải sau ngày bắt đầu.");

			StartDate = startDate;
			EndDate = endDate;
		}

		public void ChangeOrder(int newOrder)
		{
			if (newOrder < 0)
				throw DomainException.BadRequest("Thứ tự hiển thị không được âm.");

			DisplayOrder = newOrder;
		}

		public void ChangePosition(BannerPosition position) => Position = position;
		public void SetActiveStatus(bool isActive) => IsActive = isActive;

		public bool IsCurrentlyVisible(DateTime nowUtc)
		{
			if (!IsActive) return false;
			if (StartDate.HasValue && nowUtc < StartDate.Value) return false;
			if (EndDate.HasValue && nowUtc > EndDate.Value) return false;
			return true;
		}

		private static void ValidateLink(BannerLinkType type, string? target)
		{
			if (string.IsNullOrWhiteSpace(target))
				throw DomainException.BadRequest($"Đích đến liên kết là bắt buộc khi LinkType là {type}.");

			if ((type == BannerLinkType.Campaign || type == BannerLinkType.Product || type == BannerLinkType.ProductVariant)
				&& !Guid.TryParse(target, out _))
			{
				throw DomainException.BadRequest($"LinkTarget phải là một GUID hợp lệ cho loại {type}.");
			}
		}

		// --- Records ---
		public record BannerCreationPayload
		{
			public required string Title { get; init; }
			public required string ImageUrl { get; init; }
			public string? ImagePublicId { get; init; }
			public string? MobileImageUrl { get; init; }
			public string? MobileImagePublicId { get; init; }
			public string? AltText { get; init; }
			public required BannerPosition Position { get; init; }
			public required BannerLinkType LinkType { get; init; }
			public string? LinkTarget { get; init; }
		}
	}
}
