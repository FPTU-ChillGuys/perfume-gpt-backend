using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class VoucherResponse
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public Guid? CampaignId { get; set; }
		public VoucherType ApplyType { get; set; }
		public PromotionType TargetItemType { get; set; }
		public int? RequiredPoints { get; set; }
		public decimal? MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsExpired { get; set; }
		public int? TotalQuantity { get; set; }
		public int? RemainingQuantity { get; set; }
		public bool IsPublic { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
