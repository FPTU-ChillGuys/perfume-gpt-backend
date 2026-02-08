using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class GetPagedUserVouchersRequest : PagingAndSortingQuery
	{
		public UsageStatus? Status { get; set; }
		public bool? IsUsed { get; set; }
		public bool? IsExpired { get; set; }
		public string? Code { get; set; }
		public DiscountType? DiscountType { get; set; }
	}
}
