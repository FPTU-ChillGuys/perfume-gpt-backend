namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface ICampaignEndAppService
	{
		Task MarkCampaignAsEndedAsync(Guid campaignId);
	}
}
