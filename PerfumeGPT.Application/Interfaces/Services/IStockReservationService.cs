namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStockReservationService
	{
		Task ReserveStockForOrderAsync(Guid orderId, List<(Guid VariantId, int Quantity)> items, DateTime expiresAt);
		Task CommitReservationAsync(Guid orderId);
		Task ReleaseReservationAsync(Guid orderId);
		Task<int> ProcessExpiredReservationsAsync();
	}
}
