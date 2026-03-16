using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class RedeemableVoucherResponse
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsExpired { get; set; }
		public int RemainingQuantity { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
