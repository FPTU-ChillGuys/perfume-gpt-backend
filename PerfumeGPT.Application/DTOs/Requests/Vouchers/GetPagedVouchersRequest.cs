using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record GetPagedVouchersRequest : PagingAndSortingQuery
	{
		public bool? IsExpired { get; init; }
		public bool? IsRegular { get; init; }
		public string? Code { get; init; }
	}
}
