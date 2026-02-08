namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public class UpdateReviewRequest
	{
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;

		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
		public List<Guid>? MediaIdsToDelete { get; set; }
	}
}
