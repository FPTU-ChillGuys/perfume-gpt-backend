namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class CreateReturnRequestDto
	{
		public Guid OrderId { get; set; }
		public string Reason { get; set; } = string.Empty;
		public decimal RequestedRefundAmount { get; set; }
		public string? CustomerNote { get; set; }
		public List<Guid>? TemporaryMediaIds { get; set; }
	}
}
