namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record ProcessInitialReturnDto
	{
		public bool IsApproved { get; init; }
		public bool IsRequestMoreInfo { get; init; }
		public string? StaffNote { get; init; }
	}
}
