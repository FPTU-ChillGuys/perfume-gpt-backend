namespace PerfumeGPT.Application.DTOs.Responses.Reviews
{
	public record ReviewStatisticsResponse
	{
		public Guid VariantId { get; init; }
		public int TotalReviews { get; init; }
		public double AverageRating { get; init; }
		public int FiveStarCount { get; init; }
		public int FourStarCount { get; init; }
		public int ThreeStarCount { get; init; }
		public int TwoStarCount { get; init; }
		public int OneStarCount { get; init; }
	}
}
