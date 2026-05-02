using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		Task ReserveStockForOrderAsync(Order order);
		Task ReserveExactBatchStockForOrderAsync(Order order);
		Task CommitReservationAsync(Guid orderId);
		Task ReleaseOrRestockCancelledOrderAsync(Guid orderId);
		Task<(int OrdersCleaned, int ReservationsCleaned)> CleanupExpiredOrdersAndReservationsAsync();
	}
}
