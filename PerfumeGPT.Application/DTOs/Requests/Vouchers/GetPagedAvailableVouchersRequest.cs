using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public record GetPagedAvailableVouchersRequest : PagingAndSortingQuery
	{
	}
}
