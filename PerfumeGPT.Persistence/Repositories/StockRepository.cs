using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Commons;
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

		public async Task<bool> IsLowStockAsync(Guid variantId, SellableStockQueryContext sellable)
		{
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

			return await _context.Stocks
				.Where(s => s.VariantId == variantId && !s.ProductVariant.IsDeleted)
				.Select(s => new
				{
					s.LowStockThreshold,
					AvailableQuantity = s.ProductVariant.Batches
						.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
						.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0)
				})
				.Select(x => x.AvailableQuantity > 0 && x.AvailableQuantity <= x.LowStockThreshold)
				.FirstOrDefaultAsync();
		}

		public async Task<bool> HasSufficientStockAsync(Guid variantId, int requiredQuantity, int? minBufferDays = null, IEnumerable<Guid>? exemptedBatchIds = null)
		{
			var minValidDate = minBufferDays.HasValue
				? DateTime.UtcNow.AddDays(minBufferDays.Value)
				: DateTime.UtcNow;

			// 1. Xử lý List không null trước khi đưa vào LINQ
			var safeExemptedIds = exemptedBatchIds?.ToList() ?? new List<Guid>();

			var availableCommercialStock = await _context.Batches
				.Where(b => b.VariantId == variantId && !b.ProductVariant.IsDeleted)
				// LINQ giờ đây rất sạch sẽ, dễ dàng dịch thành câu lệnh IN (...)
				.Where(b => b.ExpiryDate > minValidDate || safeExemptedIds.Contains(b.Id))
				// 2. Dùng toán tử ba ngôi thay cho Math.Max để dịch thành CASE WHEN trong SQL
				.SumAsync(b => (b.RemainingQuantity - b.ReservedQuantity) > 0
					? (b.RemainingQuantity - b.ReservedQuantity)
					: 0);

			return availableCommercialStock >= requiredQuantity;
		}

		public async Task UpdateStockAsync(Guid variantId)
		{
			// Calculate total from DB plus pending tracked changes so callers are safe
			// even when they invoke this method before SaveChanges().
			var trackedBatchEntries = _context.ChangeTracker
				.Entries<Batch>()
				.Where(e => e.Entity.VariantId == variantId
					&& (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
				.ToList();

			var trackedExistingBatchIds = trackedBatchEntries
				.Where(e => e.State != EntityState.Added)
				.Select(e => e.Entity.Id)
				.ToHashSet();

			var persistedQuantity = await _context.Batches
				.Where(b => b.VariantId == variantId && !trackedExistingBatchIds.Contains(b.Id))
				.SumAsync(b => b.RemainingQuantity);

			var trackedQuantity = trackedBatchEntries
				.Where(e => e.State != EntityState.Deleted)
				.Sum(e => (int)e.CurrentValues[nameof(Batch.RemainingQuantity)]!);

			var totalQuantity = persistedQuantity + trackedQuantity;

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

		public async Task<(IEnumerable<StockResponse> Stocks, int TotalCount)> GetPagedInventoryAsync(GetPagedInventoryRequest request, SellableStockQueryContext sellable)
		{
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

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

			if (request.DaysUntilExpiry.HasValue)
			{
				var expiryDate = DateTime.UtcNow.AddDays(request.DaysUntilExpiry.Value);
				query = query.Where(s => s.ProductVariant.Batches.Any(b => b.ExpiryDate <= expiryDate));
			}

			// Apply sorting
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
				{
					nameof(Stock.Status),
					nameof(Stock.TotalQuantity),
					nameof(Stock.AvailableQuantity),
					nameof(Stock.LowStockThreshold),
					"ProductVariant.Sku",
					"ProductVariant.VolumeMl",
					"ProductVariant.Product.Name"
				};

				var trimmedSortBy = request.SortBy.Trim();
				var normalizedSortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);

				if (allowedSortColumns.Contains(normalizedSortBy) && normalizedSortBy != nameof(Stock.AvailableQuantity))
				{
					var descending = request.SortOrder?.ToLower() == "desc";
					query = query.ApplySorting(normalizedSortBy, descending);
				}
				else if (normalizedSortBy == nameof(Stock.AvailableQuantity))
				{
					var descending = request.SortOrder?.ToLower() == "desc";
					query = descending
						? query.OrderByDescending(s => s.ProductVariant.Batches
							.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
							.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0))
						: query.OrderBy(s => s.ProductVariant.Batches
							.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
							.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0));
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
			 .Select(s => new StockResponse
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
				 AvailableQuantity = s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0),
				 LowStockThreshold = s.LowStockThreshold,
				 Status = s.Status
			 })
				.ToListAsync();

			return (stocks, totalCount);
		}

		public async Task<StockResponse?> GetStockWithDetailsByVariantIdAsync(Guid variantId, SellableStockQueryContext sellable)
		{
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

			return await _context.Stocks
			.AsNoTracking()
			.Where(s => s.VariantId == variantId && !s.ProductVariant.IsDeleted) // ADDED FILTER
			.Select(s => new StockResponse
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
				AvailableQuantity = s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0),
				LowStockThreshold = s.LowStockThreshold,
				Status = s.Status
			})
			.FirstOrDefaultAsync();
		}

		public async Task<(int TotalVariants, int TotalStockQuantity, int LowStockVariantsCount)> GetInventorySummaryDataAsync(SellableStockQueryContext sellable)
		{
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

			// ADDED FILTERS to all summary calculations
			var baseQuery = _context.Stocks.Where(s => !s.ProductVariant.IsDeleted);

			var totalVariants = await baseQuery.CountAsync();
			var totalStockQuantity = await baseQuery.SumAsync(s => s.TotalQuantity);
			var lowStockVariantsCount = await baseQuery.CountAsync(s =>
				s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0) > 0
				&& s.ProductVariant.Batches
					.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
					.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0) <= s.LowStockThreshold);

			return (totalVariants, totalStockQuantity, lowStockVariantsCount);
		}

		public async Task<List<LowStockAlertItem>> GetLowStockAlertItemsAsync(SellableStockQueryContext sellable)
		{
			var normalAfter = sellable.NormalSellableAfterUtc;
			var clearanceAfter = sellable.ClearanceSellableAfterUtc;
			var clearanceBatchIds = sellable.ClearanceBatchIds;

			return await _context.Stocks
				.AsNoTracking()
				.Where(s => !s.ProductVariant.IsDeleted)
				.Select(s => new
				{
					Stock = s,
					AvailableQuantity = s.ProductVariant.Batches
						.Where(b => (b.ExpiryDate > normalAfter) || (clearanceBatchIds.Contains(b.Id) && b.ExpiryDate > clearanceAfter))
						.Sum(b => b.RemainingQuantity - b.ReservedQuantity > 0 ? b.RemainingQuantity - b.ReservedQuantity : 0)
				})
				.Where(x => x.AvailableQuantity > 0 && x.AvailableQuantity <= x.Stock.LowStockThreshold)
				.OrderBy(x => x.AvailableQuantity)
				.Select(x => new LowStockAlertItem
				{
					VariantId = x.Stock.VariantId,
					VariantSku = x.Stock.ProductVariant.Sku,
					ProductName = x.Stock.ProductVariant.Product.Name,
					TotalQuantity = x.Stock.TotalQuantity,
					AvailableQuantity = x.AvailableQuantity,
					LowStockThreshold = x.Stock.LowStockThreshold
				})
				.ToListAsync();
		}
	}
}
