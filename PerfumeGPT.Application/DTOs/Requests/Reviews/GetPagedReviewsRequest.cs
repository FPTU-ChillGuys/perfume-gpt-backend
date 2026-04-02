using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public record GetPagedReviewsRequest : PagingAndSortingQuery
	{
		public Guid? VariantId { get; init; }
		public Guid? UserId { get; init; }
		public int? MinRating { get; init; }
		public int? MaxRating { get; init; }
		public bool? HasImages { get; init; }
	}
}
