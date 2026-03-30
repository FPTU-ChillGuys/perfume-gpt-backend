namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class RecordInspectionDto
	{
		public decimal ApprovedRefundAmount { get; set; }
		public List<RecordInspectionDetailDto> InspectionResults { get; set; } = [];
	}

	public class RecordInspectionDetailDto
	{
		public Guid DetailId { get; set; }
		public bool IsRestocked { get; set; }
		public string? Note { get; set; }
	}
}
