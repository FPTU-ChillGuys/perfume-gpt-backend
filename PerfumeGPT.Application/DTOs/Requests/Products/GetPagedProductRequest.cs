using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class GetPagedProductRequest : PagingAndSortingQuery
	{
		public Gender? Gender { get; set; }
		public int? CategoryId { get; set; }
		public int? BrandId { get; set; }
		public int? Volume { get; set; }
		public decimal? FromPrice { get; set; }
		public decimal? ToPrice { get; set; }
		public bool? IsAvailable { get; set; }
	}
}
