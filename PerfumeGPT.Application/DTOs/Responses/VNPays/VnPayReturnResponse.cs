namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
	public class VnPayReturnResponse
	{
		public Guid OrderId { get; set; }
		public Guid PaymentId { get; set; }
		public bool IsSuccess { get; set; }
	}
}
