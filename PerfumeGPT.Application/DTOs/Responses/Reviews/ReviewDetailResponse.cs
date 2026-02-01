using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public class ReviewDetailResponse
	{
		public Guid Id { get; set; }
		
		// User information
		public Guid UserId { get; set; }
		public string UserFullName { get; set; } = null!;
		public string? UserProfilePictureUrl { get; set; }
		
		// Order information
		public Guid OrderDetailId { get; set; }
		public Guid OrderId { get; set; }
		public int Quantity { get; set; }
		public decimal UnitPrice { get; set; }
		
		// Variant information
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public string ProductName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		
		// Review content
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;
		public ReviewStatus Status { get; set; }
		public List<MediaResponse> Images { get; set; } = [];
		
		// Moderation information
		public Guid? ModeratedByStaffId { get; set; }
		public string? ModeratedByStaffName { get; set; }
		public DateTime? ModeratedAt { get; set; }
		public string? ModerationReason { get; set; }
		
		// Timestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}
}
