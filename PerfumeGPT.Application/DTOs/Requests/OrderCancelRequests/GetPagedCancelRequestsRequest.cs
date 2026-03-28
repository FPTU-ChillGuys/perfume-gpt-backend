using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public class GetPagedCancelRequestsRequest : PagingAndSortingQuery
	{
		public CancelRequestStatus? Status { get; set; }
       public bool? IsRefundRequired { get; set; }
	}
}
