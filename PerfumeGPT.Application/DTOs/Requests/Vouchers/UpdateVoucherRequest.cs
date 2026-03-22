using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class UpdateVoucherRequest
	{
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public VoucherType ApplyType { get; set; }
		public int RequiredPoints { get; set; }
		public decimal MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }

		public int TotalQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public bool IsPublic { get; set; }
	}
}
