using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Inventory
{
	public class GetBatchesRequest : PagingAndSortingQuery
	{
		public Guid? VariantId { get; set; }
		public string? SearchTerm { get; set; }
		public bool? IsExpired { get; set; }
		public bool? IsExpiringSoon { get; set; } // Within 30 days
	}
}
