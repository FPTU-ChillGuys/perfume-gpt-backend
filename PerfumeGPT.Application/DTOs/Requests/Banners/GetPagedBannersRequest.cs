using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Banners
{
	public record GetPagedBannersRequest : PagingAndSortingQuery
	{
		public string? SearchTerm { get; init; }
		public BannerPosition? Position { get; init; }
		public bool? IsActive { get; init; }
	}
}
