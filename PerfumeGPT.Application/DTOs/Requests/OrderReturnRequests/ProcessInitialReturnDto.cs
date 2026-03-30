namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class ProcessInitialReturnDto
	{
		public bool IsApproved { get; set; }
		public string? StaffNote { get; set; }
	}
}
