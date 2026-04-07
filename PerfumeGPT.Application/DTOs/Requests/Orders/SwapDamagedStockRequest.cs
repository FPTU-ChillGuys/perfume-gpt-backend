namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record SwapDamagedStockRequest
	{
		public Guid DamagedReservationId { get; init; }
		public int DamagedQuantity { get; init; }
		public string? DamageNote { get; init; }
	}
}
