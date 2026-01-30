using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Inventory
{
	public class GetInventoryRequest : PagingAndSortingQuery
	{
		public Guid? VariantId { get; set; }
		public string? SearchTerm { get; set; }
		public bool? IsLowStock { get; set; }
	}
}
