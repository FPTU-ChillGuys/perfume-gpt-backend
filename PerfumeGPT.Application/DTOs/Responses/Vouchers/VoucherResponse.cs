using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public record VoucherResponse
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public Guid? CampaignId { get; init; }
		public VoucherType ApplyType { get; init; }
		public PromotionType TargetItemType { get; init; }
		public int? RequiredPoints { get; init; }
        public decimal? MaxDiscountAmount { get; init; }
		public decimal? MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }
		public bool IsExpired { get; init; }
		public int? TotalQuantity { get; init; }
		public int? RemainingQuantity { get; init; }
     public int? MaxUsagePerUser { get; init; }
		public bool IsPublic { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
