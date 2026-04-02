using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public record CreateCampaignRequest
	{
		public required string Name { get; init; }
		public string? Description { get; init; }
		public DateTime StartDate { get; init; }
		public DateTime EndDate { get; init; }
		public CampaignType Type { get; init; }
		public required List<CreateCampaignPromotionItemRequest> Items { get; init; }
		public required List<CreateCampaignVoucherRequest> Vouchers { get; init; }
	}
}
