using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Banners
{
	public record BannerResponse
	{
		public Guid Id { get; init; }
		public required string Title { get; init; }
		public required string ImageUrl { get; init; }
		public string? ImagePublicId { get; init; }
		public string? MobileImageUrl { get; init; }
		public string? MobileImagePublicId { get; init; }
		public string? AltText { get; init; }
		public BannerPosition Position { get; init; }
		public int DisplayOrder { get; init; }
		public bool IsActive { get; init; }
		public DateTime? StartDate { get; init; }
		public DateTime? EndDate { get; init; }
		public BannerLinkType LinkType { get; init; }
		public string? LinkTarget { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
