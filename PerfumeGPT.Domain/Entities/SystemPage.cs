using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class SystemPage : BaseEntity<Guid>, IHasTimestamps
	{
		private SystemPage() { }
		public string Title { get; private set; } = null!;      // Tiêu đề: "Hướng dẫn mua hàng"
		public string Slug { get; private set; } = null!;         // Đường dẫn: "huong-dan-mua-hang"
		public string HtmlContent { get; private set; } = null!;  // Nội dung HTML (chứa cả text và link ảnh Supabase)
		public bool IsPublished { get; private set; }    // Trạng thái hiển thị
		public string? MetaDescription { get; private set; } // Dùng cho SEO
		public virtual ICollection<Media> PageImages { get; set; } = [];

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		public static SystemPage Create(string title, string slug, string htmlContent, bool isPublished, string? metaDescription)
		{
			return new SystemPage
			{
				Title = NormalizeRequired(title, "Tiêu đề trang là bắt buộc."),
				Slug = NormalizeSlug(slug),
				HtmlContent = NormalizeRequired(htmlContent, "Nội dung trang là bắt buộc."),
				IsPublished = isPublished,
				MetaDescription = NormalizeOptional(metaDescription)
			};
		}

		public void Update(string title, string slug, string htmlContent, string? metaDescription)
		{
			Title = NormalizeRequired(title, "Tiêu đề trang là bắt buộc.");
			Slug = NormalizeSlug(slug);
			HtmlContent = NormalizeRequired(htmlContent, "Nội dung trang là bắt buộc.");
			MetaDescription = NormalizeOptional(metaDescription);
		}

		public void Publish()
		{
			IsPublished = true;
		}

		private static string NormalizeRequired(string value, string errorMessage)
		{
			var normalized = value?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
			{
				throw DomainException.BadRequest(errorMessage);
			}

			return normalized;
		}

		private static string NormalizeSlug(string slug)
		{
			var normalized = slug?.Trim().ToLowerInvariant() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
			{
				throw DomainException.BadRequest("Slug trang là bắt buộc.");
			}

			return normalized;
		}

		private static string? NormalizeOptional(string? value)
		{
			var normalized = value?.Trim();
			return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
		}
	}
}
