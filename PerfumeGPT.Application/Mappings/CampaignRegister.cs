using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Mappings
{
	public class CampaignRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CreateCampaignRequest, Campaign.CampaignCreationFactor>()
				  .Map(dest => dest.Status, src => DateTime.UtcNow < src.StartDate ? CampaignStatus.Upcoming : CampaignStatus.Active);
		}
	}
}
