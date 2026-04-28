namespace PerfumeGPT.Application.DTOs.Requests.Pages
{
	public record UpdatePageRequest
	{
		public required string Title { get; init; }
		public required string Slug { get; init; }
		public required string HtmlContent { get; init; }
		public string? MetaDescription { get; init; }
		public List<Guid>? TemporaryMediaIdsToAdd { get; init; }
		public List<Guid>? MediaIdsToDelete { get; init; }
	}
}
