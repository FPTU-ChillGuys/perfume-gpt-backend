using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class CampaignRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Campaign, CampaignResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name)
				.Map(dest => dest.Description, src => src.Description)
				.Map(dest => dest.StartDate, src => src.StartDate)
				.Map(dest => dest.EndDate, src => src.EndDate)
				.Map(dest => dest.Type, src => src.Type)
				.Map(dest => dest.Status, src => src.Status);

			config.NewConfig<PromotionItem, CampaignPromotionItemResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CampaignId, src => src.CampaignId)
				.Map(dest => dest.ProductVariantId, src => src.ProductVariantId)
				.Map(dest => dest.BatchId, src => src.BatchId)
				.Map(dest => dest.Name, src => src.ProductVariant.Product.Name ?? string.Empty)
				.Map(dest => dest.ItemType, src => src.ItemType)
				.Map(dest => dest.StartDate, src => src.Campaign.StartDate)
				.Map(dest => dest.EndDate, src => src.Campaign.EndDate)
				.Map(dest => dest.AutoStopWhenBatchEmpty, src => src.AutoStopWhenBatchEmpty)
				.Map(dest => dest.MaxUsage, src => src.MaxUsage)
				.Map(dest => dest.CurrentUsage, src => src.CurrentUsage);
		}
	}
}
