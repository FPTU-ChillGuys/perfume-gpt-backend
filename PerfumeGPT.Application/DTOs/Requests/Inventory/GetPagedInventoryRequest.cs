using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Inventory
{
	public record GetPagedInventoryRequest : PagingAndSortingQuery
	{
		public int? CategoryId { get; init; }
		public string? BatchCode { get; init; }
		public string? SKU { get; init; }
		public int? DaysUntilExpiry { get; init; }
		public StockStatus? StockStatus { get; init; }
		public bool? IsLowStock { get; init; }
	}
}
