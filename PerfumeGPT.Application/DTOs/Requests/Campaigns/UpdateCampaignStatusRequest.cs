using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns
{
	public class UpdateCampaignStatusRequest
	{
		public CampaignStatus Status { get; set; }
	}
}
