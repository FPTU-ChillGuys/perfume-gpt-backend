using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public class ReviewResponse
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; }
		public string UserFullName { get; set; } = null!;
		public string? UserProfilePictureUrl { get; set; }
		public Guid OrderDetailId { get; set; }
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;
		public ReviewStatus Status { get; set; }
		public List<MediaResponse> Images { get; set; } = [];
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
