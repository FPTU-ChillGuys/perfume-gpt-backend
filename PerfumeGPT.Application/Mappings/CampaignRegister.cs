using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Promotions;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class CampaignRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CreateCampaignRequest, Campaign>()
				.Map(dest => dest.Name, src => src.Name.Trim())
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.StartDate, src => src.StartDate)
				.Map(dest => dest.EndDate, src => src.EndDate)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.Items, src => src.Items);

			config.NewConfig<CreateCampaignPromotionItemRequest, PromotionItem>()
				.Map(dest => dest.ProductVariantId, src => src.ProductVariantId)
				.Map(dest => dest.BatchId, src => src.BatchId)
				.Map(dest => dest.Name, src => src.Name.Trim())
				.Map(dest => dest.StartDate, src => src.StartDate)
				.Map(dest => dest.EndDate, src => src.EndDate)
				.Map(dest => dest.AutoStopWhenBatchEmpty, src => src.AutoStopWhenBatchEmpty)
				.Map(dest => dest.MaxUsage, src => src.MaxUsage)
				.Map(dest => dest.CurrentUsage, src => 0);

			config.NewConfig<UpdateCampaignRequest, Campaign>()
				.Map(dest => dest.Name, src => src.Name.Trim())
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.StartDate, src => src.StartDate)
				.Map(dest => dest.EndDate, src => src.EndDate);

			config.NewConfig<Campaign, CampaignResponse>();

			config.NewConfig<PromotionItem, CampaignPromotionItemResponse>();
		}
	}
}
