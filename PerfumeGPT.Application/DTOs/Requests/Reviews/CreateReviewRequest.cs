namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public class CreateReviewRequest
	{
		public Guid OrderDetailId { get; set; }
		public int Rating { get; set; }
		public string Comment { get; set; } = string.Empty;
		public List<Guid>? TemporaryMediaIds { get; set; }
	}
}
