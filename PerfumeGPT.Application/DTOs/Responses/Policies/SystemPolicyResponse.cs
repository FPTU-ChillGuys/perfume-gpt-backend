namespace PerfumeGPT.Application.DTOs.Responses.Policies
{
	public record SystemPolicyResponse
	{
		public required string PolicyCode { get; init; }
		public required string Title { get; init; }
		public required string HtmlContent { get; init; }
		public DateTime LastUpdated { get; init; }
	}
}
