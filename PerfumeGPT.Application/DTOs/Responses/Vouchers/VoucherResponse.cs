namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class VoucherResponse
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public string DiscountType { get; set; } = null!;
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsExpired { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
