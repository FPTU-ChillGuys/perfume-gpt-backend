namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public class CreateReturnRequestDetailDto
	{
		public Guid OrderDetailId { get; set; }
		public int ReturnedQuantity { get; set; }
	}
}
