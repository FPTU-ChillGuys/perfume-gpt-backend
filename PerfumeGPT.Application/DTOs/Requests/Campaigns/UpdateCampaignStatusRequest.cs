using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public record UpdateCampaignStatusRequest
	{
		public CampaignStatus Status { get; init; }
	}
}
