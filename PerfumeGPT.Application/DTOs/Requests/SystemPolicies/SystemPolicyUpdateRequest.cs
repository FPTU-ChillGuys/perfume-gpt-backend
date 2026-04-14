namespace PerfumeGPT.Application.DTOs.Requests.SystemPolicies
{
	public record SystemPolicyUpdateRequest
	{
		public required string Title { get; init; }
		public required string HtmlContent { get; init; }
	}
}
