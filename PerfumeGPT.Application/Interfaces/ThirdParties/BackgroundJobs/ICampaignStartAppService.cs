namespace PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs
{
	public interface ICampaignStartAppService
	{
		Task MarkCampaignAsStartedAsync(Guid campaignId);
	}
}
