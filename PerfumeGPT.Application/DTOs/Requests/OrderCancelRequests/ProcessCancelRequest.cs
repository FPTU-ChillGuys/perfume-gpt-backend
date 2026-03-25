namespace PerfumeGPT.Application.DTOs.Requests.OrderCancelRequests
{
	public class ProcessCancelRequest
	{
		public bool IsApproved { get; set; }
		public string? StaffNote { get; set; }
	}
}
