namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class UserVoucherResponse
	{
		public Guid Id { get; set; }
		public Guid VoucherId { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public string DiscountType { get; set; } = null!;
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsUsed { get; set; }
		public bool IsExpired { get; set; }
		public DateTime RedeemedAt { get; set; }
	}
}
