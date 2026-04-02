using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public record GetPagedProductRequest : PagingAndSortingQuery
	{
		public Gender? Gender { get; init; }
		public int? CategoryId { get; init; }
		public int? BrandId { get; init; }
		public int? Volume { get; init; }
		public decimal? FromPrice { get; init; }
		public decimal? ToPrice { get; init; }
		public bool? IsAvailable { get; init; }
	}
}
