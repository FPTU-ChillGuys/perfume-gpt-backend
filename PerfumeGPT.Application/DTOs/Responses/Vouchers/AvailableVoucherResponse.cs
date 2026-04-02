using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public record AvailableVoucherResponse
	{
		public Guid Id { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public decimal? MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }
		public int? RemainingQuantity { get; init; }
	}
}
