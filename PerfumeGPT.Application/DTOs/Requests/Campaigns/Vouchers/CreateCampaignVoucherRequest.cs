using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers
{
	public class CreateCampaignVoucherRequest
	{
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public PromotionType TargetItemType { get; set; }
		public DiscountType DiscountType { get; set; }
		public VoucherType ApplyType { get; set; }
	}
}
