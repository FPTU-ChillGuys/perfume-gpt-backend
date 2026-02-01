using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public class ReviewListItem
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public string UserFullName { get; set; } = null!;
		public string? UserProfilePictureUrl { get; set; }
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public int Rating { get; set; }
		public string CommentPreview { get; set; } = string.Empty;
		public ReviewStatus Status { get; set; }
		public int ImageCount { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
