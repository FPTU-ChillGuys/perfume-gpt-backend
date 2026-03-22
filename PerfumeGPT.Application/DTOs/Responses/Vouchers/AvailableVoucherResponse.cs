using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public class AvailableVoucherResponse
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = null!;
		public decimal DiscountValue { get; set; }
		public DiscountType DiscountType { get; set; }
		public decimal? MinOrderValue { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int? RemainingQuantity { get; set; }
	}
}
