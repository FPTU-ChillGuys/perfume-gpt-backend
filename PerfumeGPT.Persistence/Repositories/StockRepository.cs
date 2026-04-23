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
		public StockRepository(PerfumeDbContext context) : base(context) { }

		public async Task<bool> IsLowStockAsync(Guid variantId)
		=> await _context.Stocks
			.Where(s => s.VariantId == variantId && !s.ProductVariant.IsDeleted) // ADDED FILTER
			.Select(s => s.TotalQuantity <= s.LowStockThreshold)
			.FirstOrDefaultAsync();

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity)
		=> await _context.Stocks
			.Where(s => s.VariantId == variantId && !s.ProductVariant.IsDeleted) // ADDED FILTER
			.Select(s => (s.TotalQuantity - s.ReservedQuantity) >= requiredQuantity)
			.FirstOrDefaultAsync();

		public async Task UpdateStockAsync(Guid variantId)
		{
			// 1. Calculate total quantity from batches
			// Note: Depending on your logic, you might also want to exclude batches of deleted variants here,
			// though typically if a variant is deleted, you wouldn't be updating its stock.
			var totalQuantity = await _context.Batches
				.Where(b => b.VariantId == variantId)
				.SumAsync(b => b.RemainingQuantity);

			// 2. Load Entity (Include check for IsDeleted on the Variant)
			var stock = await _context.Stocks
				.Include(s => s.ProductVariant) // Ensure we have the variant to check
				.FirstOrDefaultAsync(s => s.VariantId == variantId)
				?? throw AppException.NotFound($"Không tìm thấy kho hàng cho biến thể sản phẩm với ID {variantId}.");

			if (stock.ProductVariant.IsDeleted)
				throw AppException.BadRequest($"Không thể cập nhật kho cho biến thể đã bị xóa (ID: {variantId}).");

			var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId)
				?? throw AppException.NotFound($"Không tìm thấy biến thể sản phẩm với ID {variantId}.");

			// 3. Sync quantity
			stock.SyncQuantity(totalQuantity);
			variant.ApplyStockPolicy(stock.TotalQuantity);
		}

		public async Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request)
		{
			IQueryable<Stock> query = _context.Stocks
				.Where(s => !s.ProductVariant.IsDeleted) // ADDED FILTER: Exclude deleted variants globally for this query
				.AsNoTracking();

			if (request.CategoryId.HasValue)
				query = query.Where(s => s.ProductVariant.Product.CategoryId == request.CategoryId.Value);

			if (request.SKU != null)
				query = query.Where(s => s.ProductVariant.Sku.Contains(request.SKU));

			if (request.BatchCode != null)
				query = query.Where(s => s.ProductVariant.Batches.Any(b => b.BatchCode.Contains(request.BatchCode)));

			if (request.StockStatus != null)
				query = query.Where(s => s.Status == request.StockStatus);

			if (request.IsLowStock == true)
				query = query.Where(s => s.TotalQuantity <= s.LowStockThreshold);

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
			 .Select(s => new AiStockResponse
			 {
				 Id = s.Id,
				 VariantId = s.VariantId,
				 VariantSku = s.ProductVariant.Sku,
				 ProductName = s.ProductVariant.Product.Name,
				 VariantImageUrl = s.ProductVariant.Media
						.Where(m => m.IsPrimary && !m.IsDeleted)
						.Select(m => m.Url)
						.FirstOrDefault() ?? string.Empty,
				 ReplenishmentPolicy = s.ProductVariant.RestockPolicy,
				 VariantStatus = s.ProductVariant.Status,
				 VolumeMl = s.ProductVariant.VolumeMl,
				 ConcentrationName = s.ProductVariant.Concentration.Name,
				 TotalQuantity = s.TotalQuantity,
				 AvailableQuantity = s.AvailableQuantity,
				 LowStockThreshold = s.LowStockThreshold,
				 BasePrice = s.ProductVariant.BasePrice,
				 Type = s.ProductVariant.Type,
				 Status = s.Status
			 })
				.ToListAsync();

			return (stocks, totalCount);
		}

		public async Task<StockResponse?> GetStockWithDetailsByVariantIdAsync(Guid variantId)
		=> await _context.Stocks
			.AsNoTracking()
			.Where(s => s.VariantId == variantId && !s.ProductVariant.IsDeleted) // ADDED FILTER
			.Select(s => new AiStockResponse
			{
				Id = s.Id,
				VariantId = s.VariantId,
				VariantSku = s.ProductVariant.Sku,
				ProductName = s.ProductVariant.Product.Name,
				VariantImageUrl = s.ProductVariant.Media
					.Where(m => m.IsPrimary && !m.IsDeleted)
					.Select(m => m.Url)
					.FirstOrDefault() ?? string.Empty,
				ReplenishmentPolicy = s.ProductVariant.RestockPolicy,
				VariantStatus = s.ProductVariant.Status,
				VolumeMl = s.ProductVariant.VolumeMl,
				ConcentrationName = s.ProductVariant.Concentration.Name,
				TotalQuantity = s.TotalQuantity,
				AvailableQuantity = s.AvailableQuantity,
				LowStockThreshold = s.LowStockThreshold,
				BasePrice = s.ProductVariant.BasePrice,
				Type = s.ProductVariant.Type,
				Status = s.Status
			})
			.FirstOrDefaultAsync();

		public async Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount, int OutOfStockVariantsCount)> GetInventorySummaryDataAsync()
		{
			// ADDED FILTERS to all summary calculations
			var baseQuery = _context.Stocks.Where(s => !s.ProductVariant.IsDeleted);

			var totalVariants = await baseQuery.CountAsync();
			var totalStockQuantity = await baseQuery.SumAsync(s => s.TotalQuantity);
			var lowStockVariantsCount = await baseQuery.CountAsync(s => s.TotalQuantity <= s.LowStockThreshold);
			var outOfStockVariantsCount = await baseQuery.CountAsync(s => s.TotalQuantity == 0);

			return (totalVariants, totalStockQuantity, lowStockVariantsCount, outOfStockVariantsCount);
		}

		public async Task<List<LowStockAlertItem>> GetLowStockAlertItemsAsync()
		=> await _context.Stocks
			.AsNoTracking()
			.Where(s => s.TotalQuantity <= s.LowStockThreshold && !s.ProductVariant.IsDeleted) // ADDED FILTER
			.OrderBy(s => s.TotalQuantity)
			.Select(s => new LowStockAlertItem
			{
				VariantId = s.VariantId,
				VariantSku = s.ProductVariant.Sku,
				ProductName = s.ProductVariant.Product.Name,
				TotalQuantity = s.TotalQuantity,
				AvailableQuantity = s.AvailableQuantity,
				LowStockThreshold = s.LowStockThreshold
			})
			.ToListAsync();
	}
}
