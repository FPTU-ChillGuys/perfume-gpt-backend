using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public record RedeemableVoucherResponse
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public int? RequiredPoints { get; init; }
		public decimal? MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }
		public bool IsExpired { get; init; }
		public int? RemainingQuantity { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
