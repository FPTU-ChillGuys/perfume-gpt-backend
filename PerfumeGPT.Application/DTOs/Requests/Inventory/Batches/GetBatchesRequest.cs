using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Inventory.Batches
{
	public record GetBatchesRequest : PagingAndSortingQuery
	{
		public Guid? VariantId { get; init; }
		public string? SearchTerm { get; init; }
		public bool? IsExpired { get; init; }
		public bool? IsExpiringSoon { get; init; } // Within 30 days
	}
}
