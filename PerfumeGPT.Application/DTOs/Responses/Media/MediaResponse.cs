namespace PerfumeGPT.Application.DTOs.Responses.Media
{
	public record MediaResponse
	{
		public Guid Id { get; init; }
		public required string Url { get; init; }
		public string? AltText { get; init; }
		public int DisplayOrder { get; init; }
		public bool IsPrimary { get; init; }
		public long? FileSize { get; init; }
		public string? MimeType { get; init; }
	}
}
