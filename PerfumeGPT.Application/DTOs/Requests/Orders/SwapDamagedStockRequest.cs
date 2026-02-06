namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class SwapDamagedStockRequest
	{
		public Guid OrderDetailId { get; set; }
		public Guid DamagedReservationId { get; set; }
		public string? DamageNote { get; set; }
	}
}
