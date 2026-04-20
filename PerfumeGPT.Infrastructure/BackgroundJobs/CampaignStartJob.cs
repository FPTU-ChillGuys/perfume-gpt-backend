using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class CampaignStartJob : ICampaignStartAppService
	{
		private readonly IUnitOfWork _unitOfWork;

		public CampaignStartJob(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task MarkCampaignAsStartedAsync(Guid campaignId)
		{
			var campaign = await _unitOfWork.Campaigns.GetCampaignWithDetailsAsync(campaignId);
			var nowUtc = DateTime.UtcNow;

			if (campaign != null && campaign.Status == CampaignStatus.Upcoming && campaign.StartDate <= nowUtc.AddMinutes(1))
			{
				campaign.UpdateStatus(CampaignStatus.Active, nowUtc);
				foreach (var item in campaign.Items)
				{
					item.SetActive(true);
				}

				_unitOfWork.Campaigns.Update(campaign);

				await _unitOfWork.SaveChangesAsync();
			}
		}
	}
}
