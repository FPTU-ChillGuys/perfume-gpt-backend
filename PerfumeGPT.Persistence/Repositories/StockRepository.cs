using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Data;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StockRepository : GenericRepository<Stock>, IStockRepository
	{
		private const int MaxRetryAttempts = 3;
		private const int RetryDelayMilliseconds = 100;

		public StockRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<bool> IsLowStockAsync(Guid variantId)
		{
			var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock == null)
			{
				return false;
			}
			return stock.TotalQuantity <= stock.LowStockThreshold;
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
			var strategy = _context.Database.CreateExecutionStrategy();

			return await strategy.ExecuteAsync(async () =>
			{
				for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
				{
					try
					{
						var totalQuantity = await _context.Batches
							.Where(b => b.VariantId == variantId && b.ExpiryDate > DateTime.UtcNow)
							.SumAsync(b => b.RemainingQuantity);

						var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
							$"UPDATE Stocks SET TotalQuantity = {totalQuantity} WHERE VariantId = {variantId}");

						return rowsAffected > 0;
					}
					catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
					{
						await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
						continue;
					}
				}

				return false;
			});
		}

		public async Task<(IEnumerable<Stock> Stocks, int TotalCount)> GetPagedInventoryAsync(
			Guid? variantId,
			string? searchTerm,
			bool? isLowStock,
			string? sortBy,
			string? sortOrder,
			int pageNumber,
			int pageSize)
		{
			IQueryable<Stock> query = _context.Stocks
				.Include(s => s.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(s => s.ProductVariant.Concentration)
				.AsNoTracking();

			// Filter by VariantId
			if (variantId.HasValue)
			{
				query = query.Where(s => s.VariantId == variantId.Value);
			}

			// Filter by SearchTerm (SKU or Product Name)
			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(s =>
					(s.ProductVariant.Sku != null && s.ProductVariant.Sku.Contains(searchTerm)) ||
					(s.ProductVariant.Product.Name != null && s.ProductVariant.Product.Name.Contains(searchTerm)));
			}

			// Filter by IsLowStock
			if (isLowStock.HasValue)
			{
				if (isLowStock.Value)
				{
					query = query.Where(s => s.TotalQuantity <= s.LowStockThreshold);
				}
				else
				{
					query = query.Where(s => s.TotalQuantity > s.LowStockThreshold);
				}
			}

			// Apply sorting
			if (!string.IsNullOrEmpty(sortBy))
			{
				var descending = sortOrder?.ToLower() == "desc";
				query = query.ApplySorting(sortBy, descending);
			}
			else
			{
				// Default sorting by product name and volume
				query = query.OrderBy(s => s.ProductVariant.Product.Name)
					.ThenBy(s => s.ProductVariant.VolumeMl);
			}

			int totalCount = await query.CountAsync();

			var stocks = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return (stocks, totalCount);
		}

		public async Task<Stock?> GetStockWithDetailsByVariantIdAsync(Guid variantId)
		{
			return await _context.Stocks
				.Include(s => s.ProductVariant)
					.ThenInclude(v => v.Product)
				.Include(s => s.ProductVariant.Concentration)
				.AsNoTracking()
				.FirstOrDefaultAsync(s => s.VariantId == variantId);
		}

		public async Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount)> GetInventorySummaryDataAsync()
		{
			var totalVariants = await _context.Stocks.CountAsync();
			var totalStockQuantity = await _context.Stocks.SumAsync(s => s.TotalQuantity);
			var lowStockVariantsCount = await _context.Stocks.CountAsync(s => s.TotalQuantity <= s.LowStockThreshold);

			return (totalVariants, totalStockQuantity, lowStockVariantsCount);
		}
	}
}
