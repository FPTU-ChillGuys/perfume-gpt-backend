using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Inventory
{
	public class GetPagedInventoryRequest : PagingAndSortingQuery
	{
		public int? CategoryId { get; set; }
		public string? BatchCode { get; set; }
		public string? SKU { get; set; }
		public StockStatus? StockStatus { get; set; }
	}
}
