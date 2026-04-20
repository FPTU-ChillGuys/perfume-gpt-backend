using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record UpdateVoucherRequest
	{
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public int RequiredPoints { get; init; }
		public decimal? MaxDiscountAmount { get; init; }
		public decimal MinOrderValue { get; init; }
		public DateTime ExpiryDate { get; init; }

		public int TotalQuantity { get; init; }
		public int RemainingQuantity { get; init; }
		public int? MaxUsagePerUser { get; init; }
		public bool IsPublic { get; init; }
		public bool IsMemberOnly { get; init; }
	}
}
