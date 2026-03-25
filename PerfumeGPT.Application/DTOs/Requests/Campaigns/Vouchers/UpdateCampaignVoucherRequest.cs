using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Campaigns.Vouchers
{
	public class UpdateCampaignVoucherRequest
	{
		public Guid? Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public PromotionType TargetItemType { get; set; }
		public DiscountType DiscountType { get; set; }
		public VoucherType ApplyType { get; set; }
	}
}
