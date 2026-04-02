using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public record ReviewDetailResponse
	{
		public Guid Id { get; init; }

		// User information
		public Guid UserId { get; init; }
		public required string UserFullName { get; init; }
		public string? UserProfilePictureUrl { get; init; }

		// Order information
		public Guid OrderDetailId { get; init; }
		public Guid OrderId { get; init; }
		public int Quantity { get; init; }
		public decimal UnitPrice { get; init; }

		// Variant information
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public required string ProductName { get; init; }
		public int VolumeMl { get; init; }
		public required string ConcentrationName { get; init; }

		// Review content
		public int Rating { get; init; }
		public required string Comment { get; init; }
		public required List<MediaResponse> Images { get; init; }

		// Staff feedback information
		public string? StaffFeedbackComment { get; init; }
		public Guid? StaffFeedbackByStaffId { get; init; }
		public DateTime? StaffFeedbackAt { get; init; }

		// Timestamps
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
