using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record GetPagedUserReturnRequestsRequest : PagingAndSortingQuery
	{

		public ReturnRequestStatus? Status { get; init; }
		public bool? IsRefunded { get; init; }
	}
}
