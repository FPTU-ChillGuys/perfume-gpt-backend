using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Pages
{
	public record PageResponse
	{
		public required string Slug { get; init; }
		public required string Title { get; init; }
		public required string HtmlContent { get; init; }
		public bool IsPublished { get; init; }
		public string? MetaDescription { get; init; }
		public required List<MediaResponse> Images { get; init; }
		public DateTime UpdatedAt { get; init; }
	}
}
