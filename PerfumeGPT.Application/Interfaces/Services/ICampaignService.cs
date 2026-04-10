using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICampaignService
	{
		Task<BaseResponse<string>> CreateCampaignAsync(CreateCampaignRequest request);
		Task<BaseResponse<PagedResult<CampaignResponse>>> GetPagedCampaignsAsync(GetPagedCampaignsRequest request);
		Task<BaseResponse<CampaignResponse>> GetCampaignByIdAsync(Guid campaignId);
		Task<BaseResponse<List<CampaignPromotionItemResponse>>> GetCampaignItemsByCampaignIdAsync(Guid campaignId);
		Task<BaseResponse<CampaignPromotionItemResponse>> GetCampaignItemByIdAsync(Guid campaignId, Guid itemId);
		Task<BaseResponse<string>> UpdateCampaignStatusAsync(Guid campaignId, UpdateCampaignStatusRequest request);
		Task<BaseResponse<string>> UpdateCampaignAsync(Guid campaignId, UpdateCampaignRequest request);
		Task<BaseResponse<string>> DeleteCampaignAsync(Guid campaignId);

		Task<BaseResponse<string>> AddCampaignItemAsync(Guid campaignId, CreateCampaignPromotionItemRequest request);
		Task<BaseResponse<string>> UpdateCampaignItemAsync(Guid campaignId, Guid itemId, UpdateCampaignPromotionItemRequest request);
		Task<BaseResponse<string>> DeleteCampaignItemAsync(Guid campaignId, Guid itemId);

		Task<BaseResponse<string>> AddCampaignVoucherAsync(Guid campaignId, CreateCampaignVoucherRequest request);
		Task<BaseResponse<List<VoucherResponse>>> GetCampaignVouchersByCampaignIdAsync(Guid campaignId);
		Task<BaseResponse<VoucherResponse>> GetCampaignVoucherByIdAsync(Guid campaignId, Guid voucherId);
		Task<BaseResponse<string>> UpdateCampaignVoucherAsync(Guid campaignId, Guid voucherId, UpdateCampaignVoucherRequest request);
		Task<BaseResponse<string>> DeleteCampaignVoucherAsync(Guid campaignId, Guid voucherId);
	}
}
