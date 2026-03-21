using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Campaigns
{
	public class CampaignPromotionItemResponse
	{
		public Guid Id { get; set; }
		public Guid CampaignId { get; set; }
		public Guid ProductVariantId { get; set; }
		public Guid? BatchId { get; set; }
		public string Name { get; set; } = string.Empty;
		public PromotionType ItemType { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public bool AutoStopWhenBatchEmpty { get; set; }
		public int? MaxUsage { get; set; }
		public int CurrentUsage { get; set; }
	}
}
