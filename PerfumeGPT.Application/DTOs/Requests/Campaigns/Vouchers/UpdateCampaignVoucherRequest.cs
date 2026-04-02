using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers
{
	public record UpdateCampaignVoucherRequest
	{
		public Guid? Id { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public PromotionType TargetItemType { get; init; }
		public DiscountType DiscountType { get; init; }
		public VoucherType ApplyType { get; init; }
	}
}
