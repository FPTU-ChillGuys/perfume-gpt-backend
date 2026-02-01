using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Reviews
{
	public class ModerateReviewRequest
	{
		public ReviewStatus Status { get; set; } // Approved or Rejected
		public string? ModerationReason { get; set; }
	}
}
