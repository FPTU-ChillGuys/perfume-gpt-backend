namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public record CreateReviewRequest
	{
		public Guid OrderDetailId { get; init; }
		public int Rating { get; init; }
		public required string Comment { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}
