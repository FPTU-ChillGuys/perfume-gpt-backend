namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class ApplyVoucherResponse
	{
		public Guid VoucherId { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountAmount { get; set; }
		public decimal FinalAmount { get; set; }
		public string DiscountType { get; set; } = null!;
	}
}
