using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Vouchers
{
	public record ApplicableVoucherResponse
	{
		public Guid VoucherId { get; init; }
		public required string Code { get; init; }
		public decimal DiscountValue { get; init; }
		public DiscountType DiscountType { get; init; }
		public bool IsApplicable { get; init; }
		public string? IneligibleReason { get; init; }
	}
}
