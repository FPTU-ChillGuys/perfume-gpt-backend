using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Promotions
{
	public class UpdateCampaignPromotionItemRequest
	{
		public Guid? Id { get; set; }
		public Guid ProductVariantId { get; set; }
		public Guid? BatchId { get; set; }
		public PromotionType PromotionType { get; set; }
		public int? MaxUsage { get; set; }
	}
}
