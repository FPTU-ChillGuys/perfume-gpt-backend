namespace PerfumeGPT.Application.DTOs.Requests.Pages
{
	public record CreatePageRequest
	{
		public required string Title { get; init; }
		public required string Slug { get; init; }
		public required string HtmlContent { get; init; }
		public bool IsPublished { get; init; }
		public string? MetaDescription { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}
