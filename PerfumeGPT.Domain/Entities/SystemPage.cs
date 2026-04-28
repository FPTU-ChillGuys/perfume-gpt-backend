using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;

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

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
