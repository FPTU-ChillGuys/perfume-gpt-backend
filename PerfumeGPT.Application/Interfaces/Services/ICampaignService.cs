using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Promotions;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICampaignService
	{
		Task<BaseResponse<string>> CreateCampaignAsync(CreateCampaignRequest request);
     Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request);
		Task<BaseResponse<CampaignResponse>> GetCampaignByIdAsync(Guid campaignId);
     Task<BaseResponse<List<CampaignPromotionItemResponse>>> GetCampaignItemsByCampaignIdAsync(Guid campaignId);
		Task<BaseResponse<string>> UpdateCampaignStatusAsync(Guid campaignId, UpdateCampaignStatusRequest request);
		Task<BaseResponse<string>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request);
		Task<BaseResponse<string>> DeleteCampaignAsync(Guid campaignId);
		Task<BaseResponse<string>> AddCampaignItemAsync(Guid campaignId, CreateCampaignPromotionItemRequest request);
		Task<BaseResponse<string>> UpdateCampaignItemAsync(Guid campaignId, Guid itemId, CreateCampaignPromotionItemRequest request);
		Task<BaseResponse<string>> DeleteCampaignItemAsync(Guid campaignId, Guid itemId);
	}
}
