using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
using PerfumeGPT.Application.Exceptions;
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

		public StockRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> IsLowStockAsync(Guid variantId)
			=> await _context.Stocks
				.Where(s => s.VariantId == variantId)
				.Select(s => s.TotalQuantity <= s.LowStockThreshold)
				.FirstOrDefaultAsync();

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
			=> await _context.Stocks
				.Where(s => s.VariantId == variantId)
				.Select(s => (s.TotalQuantity - s.ReservedQuantity) >= requiredQuantity)
				.FirstOrDefaultAsync();

		public async Task UpdateStockAsync(Guid variantId)
		{
			// 1. Calculate total quantity from batches
			var totalQuantity = await _context.Batches
				.Where(b => b.VariantId == variantId)
				.SumAsync(b => b.RemainingQuantity);

			// 2. Load Entity
			var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
				?? throw AppException.NotFound($"Stock for variant {variantId} not found.");

			// 3. Sync quantity
			stock.SyncQuantity(totalQuantity);
		}

		public async Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request)
		{
			IQueryable<Stock> query = _context.Stocks.AsNoTracking();

			if (request.CategoryId.HasValue)
				query = query.Where(s => s.ProductVariant.Product.CategoryId == request.CategoryId.Value);

			if (request.SKU != null)
				query = query.Where(s => s.ProductVariant.Sku.Contains(request.SKU));

			if (request.BatchCode != null)
				query = query.Where(s => s.ProductVariant.Batches.Any(b => b.BatchCode.Contains(request.BatchCode)));

			if (request.StockStatus != null)
				query = query.Where(s => s.Status == request.StockStatus);

			if (request.DaysUntilExpiry.HasValue)
			{
				var expiryDate = DateTime.UtcNow.AddDays(request.DaysUntilExpiry.Value);
				query = query.Where(s => s.ProductVariant.Batches.Any(b => b.ExpiryDate <= expiryDate));
			}

			// Apply sorting
			if (!string.IsNullOrEmpty(request.SortBy))
			{
				var descending = request.SortOrder?.ToLower() == "desc";
				query = query.ApplySorting(request.SortBy, descending);
			}
			else if (request.DaysUntilExpiry.HasValue)
			{
				query = query.OrderBy(s => s.ProductVariant.Batches
					.Where(b => b.ExpiryDate > DateTime.UtcNow)
					.Min(b => b.ExpiryDate));
			}
			else
			{
				query = query.OrderBy(s => s.ProductVariant.Product.Name)
					.ThenBy(s => s.ProductVariant.VolumeMl);
			}

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
				.AsNoTracking()
				.ProjectToType<StockResponse>()
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
