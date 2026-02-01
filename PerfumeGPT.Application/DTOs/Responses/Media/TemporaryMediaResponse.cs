namespace PerfumeGPT.Application.DTOs.Responses.Media
{
	public class TemporaryMediaResponse
	{
		public Guid Id { get; set; }
		public string Url { get; set; } = null!;
		public string? AltText { get; set; }
		public int DisplayOrder { get; set; }
		public long? FileSize { get; set; }
		public string? MimeType { get; set; }
		public DateTime ExpiresAt { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
