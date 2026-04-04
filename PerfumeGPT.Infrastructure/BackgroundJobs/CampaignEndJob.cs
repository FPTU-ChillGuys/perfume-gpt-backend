using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class CampaignEndJob : ICampaignEndAppService
	{
		private readonly IUnitOfWork _unitOfWork;

		public CampaignEndJob(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task MarkCampaignAsEndedAsync(Guid campaignId)
		{
			var campaign = await _unitOfWork.Campaigns.GetByIdAsync(campaignId);

			if (campaign != null && campaign.Status != CampaignStatus.Completed)
			{
				campaign.UpdateStatus(CampaignStatus.Completed, DateTime.UtcNow);
				_unitOfWork.Campaigns.Update(campaign);
				await _unitOfWork.SaveChangesAsync();
			}
		}
	}
}
