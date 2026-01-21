using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StockRepository : GenericRepository<Stock>, IStockRepository
	{
		public StockRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<bool> IsLowStockAsync(Guid variantId)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> IsValidToCart(Guid variantId, int requiredQuantity)
		{
			var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock == null)
			{
				return false;
			}
			return stock.TotalQuantity >= requiredQuantity;
		}

		public async Task<bool> UpdateStockAsync(Guid variantId)
		{
			var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			var totalQuantity = await _context.Batches
				.Where(b => b.VariantId == variantId && b.ExpiryDate > DateTime.UtcNow)
				.SumAsync(b => b.RemainingQuantity);
			if (stock != null)
			{
				stock.TotalQuantity = totalQuantity;
				await _context.SaveChangesAsync();
				return true;
			}
			return false;
		}
	}
}
