namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public class ProcessCancelRequestDto
	{
		public bool IsApproved { get; set; }
		public string? StaffNote { get; set; }
	}
}
