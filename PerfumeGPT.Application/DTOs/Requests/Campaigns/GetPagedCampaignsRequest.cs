using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public class GetPagedCampaignsRequest : PagingAndSortingQuery
	{
		public string? SearchTerm { get; set; }
		public CampaignStatus? Status { get; set; }
		public CampaignType? Type { get; set; }
	}
}
