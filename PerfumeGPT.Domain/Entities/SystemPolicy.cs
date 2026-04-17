using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class SystemPolicy : BaseEntity<string>, IUpdateAuditable, IHasCreatedAt
	{
		private SystemPolicy() { }

		public string Title { get; private set; } = null!;
		public string HtmlContent { get; private set; } = null!;
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }

		public static SystemPolicy Create(string policyCode, string title, string htmlContent)
		{
			return new SystemPolicy
			{
				Id = NormalizePolicyCode(policyCode),
				Title = NormalizeRequiredText(title, "Tiêu đề chính sách là bắt buộc."),
				HtmlContent = NormalizeRequiredText(htmlContent, "Nội dung HTML của chính sách là bắt buộc.")
			};
		}

		public void Update(string title, string htmlContent)
		{
			Title = NormalizeRequiredText(title, "Tiêu đề chính sách là bắt buộc.");
			HtmlContent = NormalizeRequiredText(htmlContent, "Nội dung HTML của chính sách là bắt buộc.");
		}

		private static string NormalizePolicyCode(string policyCode)
		{
			var normalized = policyCode?.Trim().ToUpperInvariant() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Mã chính sách là bắt buộc.");

			return normalized;
		}

		private static string NormalizeRequiredText(string value, string errorMessage)
		{
			var normalized = value?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest(errorMessage);

			return normalized;
		}
	}
}
