namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public record ReviewListItem
	{
		public Guid Id { get; init; }
		public Guid UserId { get; init; }
		public required string UserFullName { get; init; }
		public string? UserProfilePictureUrl { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public int Rating { get; init; }
		public required string CommentPreview { get; init; }
		public int ImageCount { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
