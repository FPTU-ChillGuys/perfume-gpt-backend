namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public record AnswerReviewRequest
	{
		public required string StaffFeedbackComment { get; init; }
	}
}
