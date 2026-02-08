using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockReservationRepository : IGenericRepository<StockReservation>
	{
		Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId);
		Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync();
	}
}
