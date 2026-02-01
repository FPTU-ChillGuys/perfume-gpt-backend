namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public class ReviewStatisticsResponse
	{
		public Guid VariantId { get; set; }
		public int TotalReviews { get; set; }
		public double AverageRating { get; set; }
		public int FiveStarCount { get; set; }
		public int FourStarCount { get; set; }
		public int ThreeStarCount { get; set; }
		public int TwoStarCount { get; set; }
		public int OneStarCount { get; set; }
	}
}
