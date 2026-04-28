using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Pages
{
	public record GetPagedPageRequest : PagingAndSortingQuery
	{
		public string? SearchTerm { get; init; }
	}
}
