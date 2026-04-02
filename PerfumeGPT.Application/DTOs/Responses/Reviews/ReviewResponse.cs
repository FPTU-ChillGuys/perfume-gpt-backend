using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public record ReviewResponse
	{
		public Guid Id { get; init; }
		public Guid UserId { get; init; }
		public required string UserFullName { get; init; }
		public string? UserProfilePictureUrl { get; init; }
		public Guid OrderDetailId { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public int Rating { get; init; }
		public required string Comment { get; init; }
		public string? StaffFeedbackComment { get; init; }
		public DateTime? StaffFeedbackAt { get; init; }
		public required List<MediaResponse> Images { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
