namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record RejectInspectionDto
	{
		public required string Note { get; init; }
	}
}
