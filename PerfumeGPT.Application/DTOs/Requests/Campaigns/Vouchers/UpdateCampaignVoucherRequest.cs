using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers
{
	public record UpdateCampaignVoucherRequest
	{
		public Guid? Id { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public PromotionType? TargetItemType { get; init; }
		public DiscountType DiscountType { get; init; }
		public VoucherType ApplyType { get; init; }
		public decimal? MaxDiscountAmount { get; init; }
		public required decimal MinOrderValue { get; init; }
		public int? TotalQuantity { get; init; }
		public int? MaxUsagePerUser { get; init; }
		public bool IsMemberOnly { get; init; }
	}
}
