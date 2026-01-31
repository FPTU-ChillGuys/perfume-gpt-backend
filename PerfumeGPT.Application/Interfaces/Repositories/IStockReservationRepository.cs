using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockReservationRepository : IGenericRepository<StockReservation>
	{
		/// <summary>
		/// Get all reservations for a specific order
		/// </summary>
		Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId);

		/// <summary>
		/// Get all expired reservations that haven't been released
		/// </summary>
		Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync();

		/// <summary>
		/// Get active reservations for a variant
		/// </summary>
		Task<IEnumerable<StockReservation>> GetActiveReservationsByVariantIdAsync(Guid variantId);

		/// <summary>
		/// Get total reserved quantity for a variant
		/// </summary>
		Task<int> GetTotalReservedQuantityAsync(Guid variantId);
	}
}
