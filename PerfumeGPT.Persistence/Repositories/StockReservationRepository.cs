using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StockReservationRepository : GenericRepository<StockReservation>, IStockReservationRepository
	{
		public StockReservationRepository(PerfumeDbContext context) : base(context) { }

		public async Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId)
			=> await _context.StockReservations
				.Include(sr => sr.Batch)
				.Include(sr => sr.ProductVariant)
				.Where(sr => sr.OrderId == orderId)
				.ToListAsync();

		public async Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync()
			=> await _context.StockReservations
				.Include(sr => sr.Order)
				.Include(sr => sr.Batch)
				.Include(sr => sr.ProductVariant)
				.Where(sr => sr.Status == ReservationStatus.Reserved
					&& sr.ExpiresAt <= DateTime.UtcNow)
				.ToListAsync();
	}
}
