using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Banners
{
	public record UpdateBannerRequest
	{
		public required string Title { get; init; }
        public Guid? TemporaryImageId { get; init; }
		public Guid? TemporaryMobileImageId { get; init; }
		public string? AltText { get; init; }
		public required BannerPosition Position { get; init; }
		public required int DisplayOrder { get; init; }
		public required bool IsActive { get; init; }
		public DateTime? StartDate { get; init; }
		public DateTime? EndDate { get; init; }
		public required BannerLinkType LinkType { get; init; }
		public required string LinkTarget { get; init; }
	}
}
