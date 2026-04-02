using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record CreateVoucherRequest
	{
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public VoucherType ApplyType { get; init; }
		public int RequiredPoints { get; init; }
		public decimal MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }
		public int TotalQuantity { get; init; }
		public bool IsPublic { get; init; }
	}
}
