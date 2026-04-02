using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICampaignRepository : IGenericRepository<Campaign>
	{
		Task<(List<CampaignResponse> Items, int TotalCount)> GetPagedCampaignsAsync(GetPagedCampaignsRequest request);
		Task<CampaignResponse?> GetCampaignByIdDtoAsync(Guid campaignId);
		Task<List<CampaignPromotionItemResponse>> GetCampaignItemsAsync(Guid campaignId, bool asNoTracking = true);
		Task<Campaign?> GetCampaignWithDetailsAsync(Guid campaignId);
	}
}
