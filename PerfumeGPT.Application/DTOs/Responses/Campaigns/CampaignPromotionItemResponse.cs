using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Campaigns
{
	public record CampaignPromotionItemResponse
	{
		public Guid Id { get; init; }
		public Guid CampaignId { get; init; }
		public Guid ProductVariantId { get; init; }
		public required string Sku { get; init; }
		public string? PrimaryImageUrl { get; init; }
		public required string ProductName { get; init; }
		public Guid? BatchId { get; init; }
		public string? BatchCode { get; init; }
		public PromotionType ItemType { get; init; }
		public DiscountType DiscountType { get; init; }
		public decimal DiscountValue { get; init; }
		public DateTime? StartDate { get; init; }
		public DateTime? EndDate { get; init; }
		public int? MaxUsage { get; init; }
		public int CurrentUsage { get; init; }
	}
}
