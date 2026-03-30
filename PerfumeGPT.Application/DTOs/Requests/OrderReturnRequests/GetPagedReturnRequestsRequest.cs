using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class GetPagedReturnRequestsRequest : PagingAndSortingQuery
	{
		public ReturnRequestStatus? Status { get; set; }
		public bool? IsRefunded { get; set; }
	}
}
