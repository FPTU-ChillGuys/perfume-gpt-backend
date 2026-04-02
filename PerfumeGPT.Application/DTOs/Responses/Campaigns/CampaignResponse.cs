using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Campaigns
{
	public record CampaignResponse
	{
		public Guid Id { get; init; }
		public required string Name { get; init; }
		public string? Description { get; init; }
		public DateTime StartDate { get; init; }
		public DateTime EndDate { get; init; }
		public CampaignType Type { get; init; }
		public CampaignStatus Status { get; init; }
	}
}
