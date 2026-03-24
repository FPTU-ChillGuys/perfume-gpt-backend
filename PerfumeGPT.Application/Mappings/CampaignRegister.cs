using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions;
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
				.Map(dest => dest.Items, src => src.Items)
				.Ignore(dest => dest.Vouchers);

			config.NewConfig<CreateCampaignPromotionItemRequest, PromotionItem>()
				.Map(dest => dest.ProductVariantId, src => src.ProductVariantId)
				.Map(dest => dest.ItemType, src => src.PromotionType)
				.Map(dest => dest.BatchId, src => src.BatchId)
				.Map(dest => dest.MaxUsage, src => src.MaxUsage)
				.Map(dest => dest.CurrentUsage, src => 0);

			config.NewConfig<UpdateCampaignPromotionItemRequest, PromotionItem>()
				.Map(dest => dest.ProductVariantId, src => src.ProductVariantId)
				.Map(dest => dest.ItemType, src => src.PromotionType)
				.Map(dest => dest.BatchId, src => src.BatchId)
				.Map(dest => dest.MaxUsage, src => src.MaxUsage);

			config.NewConfig<UpdateCampaignRequest, Campaign>()
				.Map(dest => dest.Name, src => src.Name.Trim())
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.StartDate, src => src.StartDate)
				.Map(dest => dest.EndDate, src => src.EndDate)
				.Map(dest => dest.Type, src => src.Type)
				.Ignore(dest => dest.Items)
				.Ignore(dest => dest.Vouchers);

			config.NewConfig<Campaign, CampaignResponse>();

			config.NewConfig<PromotionItem, CampaignPromotionItemResponse>();
		}
	}
}
