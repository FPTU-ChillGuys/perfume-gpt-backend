using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class CreateVoucherRequest
	{
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public long RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
	}
}
