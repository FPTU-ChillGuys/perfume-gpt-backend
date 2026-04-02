using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Carts
{
	public record GetPagedCartItemsRequest : PagingAndSortingQuery
	{
		public List<Guid>? ItemIds { get; init; }
	}
}
