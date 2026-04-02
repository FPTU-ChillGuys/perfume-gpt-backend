namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record StartInspectionDto
	{
		public string? InspectionNote { get; init; }
	}
}
