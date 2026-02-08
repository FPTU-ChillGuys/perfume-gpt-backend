using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
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

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
		{
			var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock == null)
			{
				return false;
			}
			return stock.AvailableQuantity >= requiredQuantity;
		}

		//public async Task<bool> UpdateStockAsync(Guid variantId)
		//{
		//	var strategy = _context.Database.CreateExecutionStrategy();

		//	return await strategy.ExecuteAsync(async () =>
		//	{
		//		for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
		//		{
		//			try
		//			{
		//				var totalQuantity = await _context.Batches
		//					.Where(b => b.VariantId == variantId && b.ExpiryDate > DateTime.UtcNow)
		//					.SumAsync(b => b.RemainingQuantity);

		//				var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
		//					$"UPDATE Stocks SET TotalQuantity = {totalQuantity} WHERE VariantId = {variantId}");

		//				return rowsAffected > 0;
		//			}
		//			catch (DbUpdateConcurrencyException) when (attempt < MaxRetryAttempts - 1)
		//			{
		//				await Task.Delay(RetryDelayMilliseconds * (attempt + 1));
		//				continue;
		//			}
		//		}

		//		return false;
		//	});
		//}

		public async Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request)
		{
			IQueryable<Stock> query = _context.Stocks
				.AsNoTracking();

			// Filter by VariantId
			if (request.VariantId.HasValue)
			{
				query = query.Where(s => s.VariantId == request.VariantId.Value);
			}

			// Filter by SearchTerm (SKU or Product Name)
			if (!string.IsNullOrEmpty(request.SearchTerm))
			{
				query = query.Where(s =>
					(s.ProductVariant.Sku != null && s.ProductVariant.Sku.Contains(request.SearchTerm)) ||
					(s.ProductVariant.Product.Name != null && s.ProductVariant.Product.Name.Contains(request.SearchTerm)));
			}

			// Filter by IsLowStock
			if (request.IsLowStock.HasValue)
			{
				if (request.IsLowStock.Value)
				{
					query = query.Where(s => s.TotalQuantity <= s.LowStockThreshold);
				}
				else
				{
					query = query.Where(s => s.TotalQuantity > s.LowStockThreshold);
				}
			}

			// Apply sorting
			if (!string.IsNullOrEmpty(request.SortBy))
			{
				var descending = request.SortOrder?.ToLower() == "desc";
				query = query.ApplySorting(request.SortBy, descending);
			}
			else
			{
				// Default sorting by product name and volume
				query = query.OrderBy(s => s.ProductVariant.Product.Name)
					.ThenBy(s => s.ProductVariant.VolumeMl);
			}

			// Get total count before pagination
			int totalCount = await query.CountAsync();

			var stocks = await query
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<StockResponse>()
				.ToListAsync();

			return (stocks, totalCount);
		}

		public async Task<StockResponse?> GetStockWithDetailsByVariantIdAsync(Guid variantId)
		{
			return await _context.Stocks
				.ProjectToType<StockResponse>()
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
