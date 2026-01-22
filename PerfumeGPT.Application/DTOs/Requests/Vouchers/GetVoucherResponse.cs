namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class GetVoucherResponse
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public string? DiscountType { get; set; } // e.g., "Percentage", "FixedAmount"
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
	}
}
