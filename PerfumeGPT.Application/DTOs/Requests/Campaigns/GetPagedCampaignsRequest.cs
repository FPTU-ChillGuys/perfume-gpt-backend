using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public record GetPagedCampaignsRequest : PagingAndSortingQuery
	{
		public string? SearchTerm { get; init; }
		public CampaignStatus? Status { get; init; }
		public CampaignType? Type { get; init; }
	}
}
