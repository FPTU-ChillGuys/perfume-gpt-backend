using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public class UpdateCampaignRequest
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public CampaignType Type { get; set; }
		public List<UpdateCampaignPromotionItemRequest> Items { get; set; } = [];
		public List<UpdateCampaignVoucherRequest> Vouchers { get; set; } = [];
	}
}
