using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers
{
	public record CreateCampaignVoucherRequest
	{
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public PromotionType TargetItemType { get; init; }
		public DiscountType DiscountType { get; init; }
		public VoucherType ApplyType { get; init; }
	}
}
