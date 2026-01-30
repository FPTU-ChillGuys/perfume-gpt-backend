namespace PerfumeGPT.Application.DTOs.Responses.Media
{
	public class MediaResponse
	{
		public Guid Id { get; set; }
		public string Url { get; set; } = null!;
		public string? AltText { get; set; }
		public int DisplayOrder { get; set; }
		public bool IsPrimary { get; set; }
		public long? FileSize { get; set; }
		public string? MimeType { get; set; }
	}
}
