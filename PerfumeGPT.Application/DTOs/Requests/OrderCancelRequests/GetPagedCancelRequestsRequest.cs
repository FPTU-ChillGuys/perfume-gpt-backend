using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public record GetPagedCancelRequestsRequest : PagingAndSortingQuery
	{
		public CancelRequestStatus? Status { get; init; }
		public bool? IsRefundRequired { get; init; }
	}
}
