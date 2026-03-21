using PerfumeGPT.Application.DTOs.Requests.Promotions;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public class CreateCampaignRequest
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public CampaignType Type { get; set; }
		public List<CreateCampaignPromotionItemRequest> Items { get; set; } = [];
	}
}
