using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public class GetPagedReviewsRequest : PagingAndSortingQuery
	{
		public Guid? VariantId { get; set; }
		public Guid? UserId { get; set; }
		public ReviewStatus? Status { get; set; }
		public int? MinRating { get; set; }
		public int? MaxRating { get; set; }
		public bool? HasImages { get; set; }
	}
}
