using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class GetPagedVouchersRequest : PagingAndSortingQuery
	{
		public bool? IsExpired { get; set; }
		public string? Code { get; set; }
	}
}
