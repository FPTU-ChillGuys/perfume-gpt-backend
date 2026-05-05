namespace PerfumeGPT.Application.DTOs.Responses.Campaigns
{
	public record CampaignLookupItem
	{
		public Guid Id { get; init; }
		public required string Name { get; init; }
		public DateTime StartDate { get; init; }
		public DateTime EndDate { get; init; }
	}
}
