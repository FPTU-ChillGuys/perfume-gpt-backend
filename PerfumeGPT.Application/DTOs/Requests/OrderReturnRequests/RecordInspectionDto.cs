namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record RecordInspectionDto
	{
		public decimal ApprovedRefundAmount { get; init; }
		public bool IsRestocked { get; init; }
		public string? InspectionNote { get; init; }
	}
}
