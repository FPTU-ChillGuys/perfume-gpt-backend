using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record GetPagedUserVouchersRequest : PagingAndSortingQuery
	{
		public UsageStatus? Status { get; init; }
		public bool? IsUsed { get; init; }
		public bool? IsExpired { get; init; }
		public string? Code { get; init; }
		public DiscountType? DiscountType { get; init; }
	}
}
