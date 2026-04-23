namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		Task ReserveStockForOrderAsync(Guid orderId, List<(Guid VariantId, int Quantity)> items, DateTime? expiresAt);
		Task ReserveExactBatchStockForOrderAsync(Guid orderId, List<(Guid VariantId, Guid BatchId, int Quantity)> items, DateTime? expiresAt);
		Task CommitReservationAsync(Guid orderId);
		Task ReleaseOrRestockCancelledOrderAsync(Guid orderId);
		Task<(int OrdersCleaned, int ReservationsCleaned)> CleanupExpiredOrdersAndReservationsAsync();
	}
}
