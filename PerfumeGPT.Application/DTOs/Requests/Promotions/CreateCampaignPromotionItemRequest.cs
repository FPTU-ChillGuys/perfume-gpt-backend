using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Promotions
{
	public class CreateCampaignPromotionItemRequest
	{
		public Guid ProductVariantId { get; set; }
		public Guid? BatchId { get; set; }
		public string Name { get; set; } = string.Empty;
		public PromotionType PromotionType { get; set; }
		public DateTime? StartDate { get; set; }
		public DateTime? EndDate { get; set; }
		public bool AutoStopWhenBatchEmpty { get; set; }
		public int? MaxUsage { get; set; }
	}
}
