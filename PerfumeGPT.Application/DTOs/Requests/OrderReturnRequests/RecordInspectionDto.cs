namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class RecordInspectionDto
	{
		public decimal ApprovedRefundAmount { get; set; }
		public bool IsRestocked { get; set; }
		public string? InspectionNote { get; set; }
	}
}
