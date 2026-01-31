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
		public StockReservationRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<IEnumerable<StockReservation>> GetByOrderIdAsync(Guid orderId)
		{
			return await _context.StockReservations
				.Include(sr => sr.Batch)
				.Include(sr => sr.ProductVariant)
				.Where(sr => sr.OrderId == orderId)
				.ToListAsync();
		}

		public async Task<IEnumerable<StockReservation>> GetExpiredReservationsAsync()
		{
			var now = DateTime.UtcNow;
			return await _context.StockReservations
				.Include(sr => sr.Order)
				.Include(sr => sr.Batch)
				.Include(sr => sr.ProductVariant)
				.Where(sr => sr.Status == ReservationStatus.Reserved
					&& sr.ExpiresAt.HasValue
					&& sr.ExpiresAt.Value <= now)
				.ToListAsync();
		}

		public async Task<IEnumerable<StockReservation>> GetActiveReservationsByVariantIdAsync(Guid variantId)
		{
			return await _context.StockReservations
				.Include(sr => sr.Batch)
				.Where(sr => sr.VariantId == variantId
					&& sr.Status == ReservationStatus.Reserved)
				.ToListAsync();
		}

		public async Task<int> GetTotalReservedQuantityAsync(Guid variantId)
		{
			return await _context.StockReservations
				.Where(sr => sr.VariantId == variantId
					&& sr.Status == ReservationStatus.Reserved)
				.SumAsync(sr => sr.ReservedQuantity);
		}
	}
}
