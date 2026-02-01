namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public class UpdateReviewRequest
	{
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;

		// Image management
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; } // New images to add (from temp upload)
		public List<Guid>? MediaIdsToDelete { get; set; } // Existing images to remove
	}
}
