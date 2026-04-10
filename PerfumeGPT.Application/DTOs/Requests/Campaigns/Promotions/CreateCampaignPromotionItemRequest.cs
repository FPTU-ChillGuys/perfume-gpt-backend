using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions
{
	public record CreateCampaignPromotionItemRequest
	{
		public Guid ProductVariantId { get; init; }
		public Guid? BatchId { get; init; }
		public PromotionType PromotionType { get; init; }
		public DiscountType DiscountType { get; init; }
		public decimal DiscountValue { get; init; }
		public int? MaxUsage { get; init; }
	}
}
