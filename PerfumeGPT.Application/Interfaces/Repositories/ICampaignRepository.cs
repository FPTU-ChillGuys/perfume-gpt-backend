using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICampaignRepository : IGenericRepository<Campaign>
	{
		Task<(IEnumerable<Campaign> Items, int TotalCount)> GetPagedCampaignsAsync(GetPagedCampaignsRequest request);
		Task<IEnumerable<PromotionItem>> GetCampaignItemsAsync(Guid campaignId, bool asNoTracking = true);
		Task<Campaign?> GetCampaignWithDetailsAsync(Guid campaignId);
	}
}
