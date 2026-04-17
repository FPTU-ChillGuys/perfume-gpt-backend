using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public record UserVoucherResponse
	{
		public Guid Id { get; init; }
		public Guid VoucherId { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public decimal? MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }
		public bool IsUsed { get; init; }
		public UsageStatus Status { get; init; }
		public bool IsExpired { get; init; }
		public DateTime RedeemedAt { get; init; }
	}
}
